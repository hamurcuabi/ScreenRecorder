// This file is part of ScreenRecorder
//  
// ScreenRecorder  is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ScreenRecorder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ScreenRecorder.  If not, see <http://www.gnu.org/licenses/>.

#region .NET Disclaimer/Info

//===============================================================================
//
// gOODiDEA, uland.com
//===============================================================================
//
// $Header :		$  
// $Author :		$
// $Date   :		$
// $Revision:		$
// $History:		$  
//  
//===============================================================================

#endregion

#region Java

/* NeuQuant Neural-Net Quantization Algorithm
 * ------------------------------------------
 *
 * Copyright (c) 1994 Anthony Dekker
 *
 * NEUQUANT Neural-Net quantization algorithm by Anthony Dekker, 1994.
 * See "Kohonen neural networks for optimal colour quantization"
 * in "Network: Computation in Neural Systems" Vol. 5 (1994) pp 351-367.
 * for a discussion of the algorithm.
 *
 * Any party obtaining a copy of these files from the author, directly or
 * indirectly, is granted, free of charge, a full and unrestricted irrevocable,
 * world-wide, paid up, royalty-free, nonexclusive right and license to deal
 * in this software and documentation files (the "Software"), including without
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons who receive
 * copies from any such party to do so, with the only requirement being
 * that this copyright notice remain intact.
 */

// Ported to Java 12/00 K Weiner

#endregion

#region Using directives

using System;

#endregion

namespace ScreenRecorder.Codecs
{
    /// <summary>
    ///     Neu quant
    /// </summary>
    public class NeuQuant
    {
        /// <summary>
        ///     Netsize
        /// </summary>
        protected static readonly int netsize = 256; /* number of colours used */

        /* four primes near 500 - assume no image has a length so large */
        /* that it is divisible by all four primes */

        /// <summary>
        ///     Prime 1
        /// </summary>
        protected static readonly int prime1 = 499;

        /// <summary>
        ///     Prime 2
        /// </summary>
        protected static readonly int prime2 = 491;

        /// <summary>
        ///     Prime 3
        /// </summary>
        protected static readonly int prime3 = 487;

        /// <summary>
        ///     Prime 4
        /// </summary>
        protected static readonly int prime4 = 503;

        /// <summary>
        ///     Minpicturebytes
        /// </summary>
        protected static readonly int minpicturebytes = (3*prime4);

        /* minimum size for input image */
        /* Program Skeleton
		   ----------------
		   [select samplefac in range 1..30]
		   [read image from input file]
		   pic = (unsigned char*) malloc(3*width*height);
		   initnet(pic,3*width*height,samplefac);
		   learn();
		   unbiasnet();
		   [write output image header, using writecolourmap(f)]
		   inxbuild();
		   write output image using inxsearch(b,g,r)      */

        /* Network Definitions
		   ------------------- */

        /// <summary>
        ///     Maxnetpos
        /// </summary>
        protected static readonly int maxnetpos = (netsize - 1);

        /// <summary>
        ///     Netbiasshift
        /// </summary>
        protected static readonly int netbiasshift = 4; /* bias for colour values */

        /// <summary>
        ///     Ncycles
        /// </summary>
        protected static readonly int ncycles = 100; /* no. of learning cycles */

        /* defs for freq and bias */

        /// <summary>
        ///     Intbiasshift
        /// </summary>
        protected static readonly int intbiasshift = 16; /* bias for fractions */

        /// <summary>
        ///     Intbias
        /// </summary>
        protected static readonly int intbias = (((int) 1) << intbiasshift);

        /// <summary>
        ///     Gammashift
        /// </summary>
        protected static readonly int gammashift = 10; /* gamma = 1024 */

        /// <summary>
        ///     Gamma
        /// </summary>
        protected static readonly int gamma = (((int) 1) << gammashift);

        protected static readonly int betashift = 10;
        protected static readonly int beta = (intbias >> betashift); /* beta = 1/1024 */

        protected static readonly int betagamma =
            (intbias << (gammashift - betashift));

        /* defs for decreasing radius factor */
        protected static readonly int initrad = (netsize >> 3); /* for 256 cols, radius starts */
        protected static readonly int radiusbiasshift = 6; /* at 32.0 biased by 6 bits */
        protected static readonly int radiusbias = (((int) 1) << radiusbiasshift);
        protected static readonly int initradius = (initrad*radiusbias); /* and decreases by a */
        protected static readonly int radiusdec = 30; /* factor of 1/30 each cycle */

        /* defs for decreasing alpha factor */
        protected static readonly int alphabiasshift = 10; /* alpha starts at 1.0 */
        protected static readonly int initalpha = (((int) 1) << alphabiasshift);

        /* radbias and alpharadbias used for radpower calculation */
        protected static readonly int radbiasshift = 8;
        protected static readonly int radbias = (((int) 1) << radbiasshift);
        protected static readonly int alpharadbshift = (alphabiasshift + radbiasshift);
        protected static readonly int alpharadbias = (((int) 1) << alpharadbshift);
        protected int alphadec; /* biased by 10 bits */

        /* Types and Global Variables
		-------------------------- */

        /* for network lookup - really 256 */

        protected int[] bias = new int[netsize];
        /* bias and freq arrays for learning */
        protected int[] freq = new int[netsize];
        protected int lengthcount; /* lengthcount = H*W*3 */
        protected int[] netindex = new int[256];
        protected int[][] network; /* the network itself - [netsize][4] */
        protected int[] radpower = new int[initrad];
        protected int samplefac; /* sampling factor 1..30 */
        protected byte[] thepicture; /* the input image itself */
        /* radpower for precomputation */

        /* Initialise network in range (0,0,0) to (255,255,255) and set parameters
		   ----------------------------------------------------------------------- */

        public NeuQuant(byte[] thepic, int len, int sample)
        {
            int i;
            int[] p;

            thepicture = thepic;
            lengthcount = len;
            samplefac = sample;

            network = new int[netsize][];
            for (i = 0; i < netsize; i++)
            {
                network[i] = new int[4];
                p = network[i];
                p[0] = p[1] = p[2] = (i << (netbiasshift + 8))/netsize;
                freq[i] = intbias/netsize; /* 1/netsize */
                bias[i] = 0;
            }
        }

        public byte[] ColorMap()
        {
            var map = new byte[3*netsize];
            var index = new int[netsize];
            for (var i = 0; i < netsize; i++)
                index[network[i][3]] = i;
            var k = 0;
            for (var i = 0; i < netsize; i++)
            {
                var j = index[i];
                map[k++] = (byte) (network[j][0]);
                map[k++] = (byte) (network[j][1]);
                map[k++] = (byte) (network[j][2]);
            }
            return map;
        }

        /* Insertion sort of network and building of netindex[0..255] (to do after unbias)
		   ------------------------------------------------------------------------------- */

        public void Inxbuild()
        {
            int i, j, smallpos, smallval;
            int[] p;
            int[] q;
            var startpos = 0;

            var previouscol = 0;
            for (i = 0; i < netsize; i++)
            {
                p = network[i];
                smallpos = i;
                smallval = p[1]; /* index on g */
                /* find smallest in i..netsize-1 */
                for (j = i + 1; j < netsize; j++)
                {
                    q = network[j];
                    if (q[1] < smallval)
                    {
                        /* index on g */
                        smallpos = j;
                        smallval = q[1]; /* index on g */
                    }
                }
                q = network[smallpos];
                /* swap p (i) and q (smallpos) entries */
                if (i != smallpos)
                {
                    j = q[0];
                    q[0] = p[0];
                    p[0] = j;
                    j = q[1];
                    q[1] = p[1];
                    p[1] = j;
                    j = q[2];
                    q[2] = p[2];
                    p[2] = j;
                    j = q[3];
                    q[3] = p[3];
                    p[3] = j;
                }
                /* smallval entry is now in position i */
                if (smallval != previouscol)
                {
                    netindex[previouscol] = (startpos + i) >> 1;
                    for (j = previouscol + 1; j < smallval; j++)
                        netindex[j] = i;
                    previouscol = smallval;
                    startpos = i;
                }
            }
            netindex[previouscol] = (startpos + maxnetpos) >> 1;
            for (j = previouscol + 1; j < 256; j++)
                netindex[j] = maxnetpos; /* really 256 */
        }

        /* Main Learning Loop
		   ------------------ */

        public void Learn()
        {
            int i, j, b, g, r;
            int radius, rad, alpha, step, delta, samplepixels;
            byte[] p;
            int lim;

            if (lengthcount < minpicturebytes)
                samplefac = 1;
            alphadec = 30 + ((samplefac - 1)/3);
            p = thepicture;
            var pix = 0;
            lim = lengthcount;
            samplepixels = lengthcount/(3*samplefac);
            delta = samplepixels/ncycles;
            alpha = initalpha;
            radius = initradius;

            rad = radius >> radiusbiasshift;
            if (rad <= 1)
                rad = 0;
            for (i = 0; i < rad; i++)
                radpower[i] =
                    alpha*(((rad*rad - i*i)*radbias)/(rad*rad));

            //fprintf(stderr,"beginning 1D learning: initial radius=%d\n", rad);

            if (lengthcount < minpicturebytes)
                step = 3;
            else if ((lengthcount%prime1) != 0)
                step = 3*prime1;
            else
            {
                if ((lengthcount%prime2) != 0)
                    step = 3*prime2;
                else
                {
                    if ((lengthcount%prime3) != 0)
                        step = 3*prime3;
                    else
                        step = 3*prime4;
                }
            }

            i = 0;
            while (i < samplepixels)
            {
                b = (p[pix + 0] & 0xff) << netbiasshift;
                g = (p[pix + 1] & 0xff) << netbiasshift;
                r = (p[pix + 2] & 0xff) << netbiasshift;
                j = Contest(b, g, r);

                Altersingle(alpha, j, b, g, r);
                if (rad != 0)
                    Alterneigh(rad, j, b, g, r); /* alter neighbours */

                pix += step;
                if (pix >= lim)
                    pix -= lengthcount;

                i++;
                if (delta == 0)
                    delta = 1;
                if (i%delta == 0)
                {
                    alpha -= alpha/alphadec;
                    radius -= radius/radiusdec;
                    rad = radius >> radiusbiasshift;
                    if (rad <= 1)
                        rad = 0;
                    for (j = 0; j < rad; j++)
                        radpower[j] =
                            alpha*(((rad*rad - j*j)*radbias)/(rad*rad));
                }
            }
            //fprintf(stderr,"finished 1D learning: readonly alpha=%f !\n",((float)alpha)/initalpha);
        }

        /* Search for BGR values 0..255 (after net is unbiased) and return colour index
		   ---------------------------------------------------------------------------- */

        public int Map(int b, int g, int r)
        {
            int i, j, dist, a;
            int[] p;
            var best = -1;

            var bestd = 1000; /* biggest possible dist is 256*3 */
            i = netindex[g]; /* index on g */
            j = i - 1; /* start at netindex[g] and work outwards */

            while ((i < netsize) || (j >= 0))
            {
                if (i < netsize)
                {
                    p = network[i];
                    dist = p[1] - g; /* inx key */
                    if (dist >= bestd)
                        i = netsize; /* stop iter */
                    else
                    {
                        i++;
                        if (dist < 0)
                            dist = -dist;
                        a = p[0] - b;
                        if (a < 0)
                            a = -a;
                        dist += a;
                        if (dist < bestd)
                        {
                            a = p[2] - r;
                            if (a < 0)
                                a = -a;
                            dist += a;
                            if (dist < bestd)
                            {
                                bestd = dist;
                                best = p[3];
                            }
                        }
                    }
                }
                if (j >= 0)
                {
                    p = network[j];
                    dist = g - p[1]; /* inx key - reverse dif */
                    if (dist >= bestd)
                        j = -1; /* stop iter */
                    else
                    {
                        j--;
                        if (dist < 0)
                            dist = -dist;
                        a = p[0] - b;
                        if (a < 0)
                            a = -a;
                        dist += a;
                        if (dist < bestd)
                        {
                            a = p[2] - r;
                            if (a < 0)
                                a = -a;
                            dist += a;
                            if (dist < bestd)
                            {
                                bestd = dist;
                                best = p[3];
                            }
                        }
                    }
                }
            }
            return (best);
        }

        public byte[] Process()
        {
            Learn();
            Unbiasnet();
            Inxbuild();
            return ColorMap();
        }

        /* Unbias network to give byte values 0..255 and record position i to prepare for sort
		   ----------------------------------------------------------------------------------- */

        public void Unbiasnet()
        {
            int i;

            for (i = 0; i < netsize; i++)
            {
                network[i][0] >>= netbiasshift;
                network[i][1] >>= netbiasshift;
                network[i][2] >>= netbiasshift;
                network[i][3] = i; /* record colour no */
            }
        }

        /* Move adjacent neurons by precomputed alpha*(1-((i-j)^2/[r]^2)) in radpower[|i-j|]
		   --------------------------------------------------------------------------------- */

        protected void Alterneigh(int rad, int i, int b, int g, int r)
        {
            int j, k, lo, hi, a;
            int[] p;

            lo = i - rad;
            if (lo < -1)
                lo = -1;
            hi = i + rad;
            if (hi > netsize)
                hi = netsize;

            j = i + 1;
            k = i - 1;
            var m = 1;
            while ((j < hi) || (k > lo))
            {
                a = radpower[m++];
                if (j < hi)
                {
                    p = network[j++];
                    try
                    {
                        p[0] -= (a*(p[0] - b))/alpharadbias;
                        p[1] -= (a*(p[1] - g))/alpharadbias;
                        p[2] -= (a*(p[2] - r))/alpharadbias;
                    }
                    catch (Exception e)
                    {
                    } // prevents 1.3 miscompilation
                }
                if (k > lo)
                {
                    p = network[k--];
                    try
                    {
                        p[0] -= (a*(p[0] - b))/alpharadbias;
                        p[1] -= (a*(p[1] - g))/alpharadbias;
                        p[2] -= (a*(p[2] - r))/alpharadbias;
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        /* Move neuron i towards biased (b,g,r) by factor alpha
		   ---------------------------------------------------- */

        protected void Altersingle(int alpha, int i, int b, int g, int r)
        {
            /* alter hit neuron */
            var n = network[i];
            n[0] -= (alpha*(n[0] - b))/initalpha;
            n[1] -= (alpha*(n[1] - g))/initalpha;
            n[2] -= (alpha*(n[2] - r))/initalpha;
        }

        /* Search for biased BGR values
		   ---------------------------- */

        protected int Contest(int b, int g, int r)
        {
            /* finds closest neuron (min dist) and updates freq */
            /* finds best neuron (min dist-bias) and returns position */
            /* for frequently chosen neurons, freq[i] is high and bias[i] is negative */
            /* bias[i] = gamma*((1/netsize)-freq[i]) */

            int i, dist, a, biasdist, betafreq;
            int bestbiaspos, bestbiasd;
            int[] n;

            var bestd = ~(((int) 1) << 31);
            bestbiasd = bestd;
            var bestpos = -1;
            bestbiaspos = bestpos;

            for (i = 0; i < netsize; i++)
            {
                n = network[i];
                dist = n[0] - b;
                if (dist < 0)
                    dist = -dist;
                a = n[1] - g;
                if (a < 0)
                    a = -a;
                dist += a;
                a = n[2] - r;
                if (a < 0)
                    a = -a;
                dist += a;
                if (dist < bestd)
                {
                    bestd = dist;
                    bestpos = i;
                }
                biasdist = dist - ((bias[i]) >> (intbiasshift - netbiasshift));
                if (biasdist < bestbiasd)
                {
                    bestbiasd = biasdist;
                    bestbiaspos = i;
                }
                betafreq = (freq[i] >> betashift);
                freq[i] -= betafreq;
                bias[i] += (betafreq << gammashift);
            }
            freq[bestpos] += beta;
            bias[bestpos] -= betagamma;
            return (bestbiaspos);
        }
    } // class NeuQuant
} // namespace Gif.Components