// Copyright (C) 2011 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MyMediaLite.DataType;
using MyMediaLite.Eval;
using MyMediaLite.Util;

namespace MyMediaLite.ItemRecommendation
{
	/// <summary>
	/// Matrix factorization model for item prediction optimized for a soft margin ranking loss,
	/// using stochastic gradient descent (as in BPR-MF).
	/// </summary>
	/// <remarks>
	/// Literature:
	/// <list type="bullet">
    ///   <item><description>
    ///     Markus Weimer, Alexandros Karatzoglou, Alex Smola:
    ///     Improving Maximum Margin Matrix Factorization.
    ///     Machine Learning Journal 2008.
	///   </description></item>
    ///   <item><description>
	///     Steffen Rendle, Christoph Freudenthaler, Zeno Gantner, Lars Schmidt-Thieme:
	///     BPR: Bayesian Personalized Ranking from Implicit Feedback.
	///     UAI 2009.
	///     http://www.ismll.uni-hildesheim.de/pub/pdfs/Rendle_et_al2009-Bayesian_Personalized_Ranking.pdf
	///   </description></item>
    /// </list>
    /// 
	/// This recommender supports incremental updates.
	/// </remarks>
	public class SoftMarginRankingMF : BPRMF, IIterativeModel
	{
		public SoftMarginRankingMF() : base()
		{
			LearnRate = 0.1;
		}
		
		/// <summary>Update latent factors according to the stochastic gradient descent update rule</summary>
		/// <param name="u">the user ID</param>
		/// <param name="i">the ID of the first item</param>
		/// <param name="j">the ID of the second item</param>
		/// <param name="update_u">if true, update the user latent factors</param>
		/// <param name="update_i">if true, update the latent factors of the first item</param>
		/// <param name="update_j">if true, update the latent factors of the second item</param>
		protected override void UpdateFactors(int u, int i, int j, bool update_u, bool update_i, bool update_j)
		{
			double x_uij = item_bias[i] - item_bias[j] + MatrixUtils.RowScalarProductWithRowDifference(user_factors, u, item_factors, i, item_factors, j);

			double common_part = x_uij < 0 ? 1 : 0;

			// adjust bias terms
			if (update_i)
			{
				double bias_update = common_part - BiasReg * item_bias[i];
				item_bias[i] += learn_rate * bias_update;
			}

			if (update_j)
			{
				double bias_update = -common_part - BiasReg * item_bias[j];
				item_bias[j] += learn_rate * bias_update;
			}

			// adjust factors
			for (int f = 0; f < num_factors; f++)
			{
				double w_uf = user_factors[u, f];
				double h_if = item_factors[i, f];
				double h_jf = item_factors[j, f];

				if (update_u)
				{
					double uf_update = (h_if - h_jf) * common_part - reg_u * w_uf;
					user_factors[u, f] = w_uf + learn_rate * uf_update;
				}

				if (update_i)
				{
					double if_update = w_uf * common_part - reg_i * h_if;
					item_factors[i, f] = h_if + learn_rate * if_update;
				}

				if (update_j)
				{
					double jf_update = -w_uf  * common_part - reg_j * h_jf;
					item_factors[j, f] = h_jf + learn_rate * jf_update;
				}
			}
		}

		/// <summary>Compute approximate loss</summary>
		/// <returns>the approximate loss</returns>
		public override double ComputeLoss()
		{
			throw new NotImplementedException();
		}

		///
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0} num_factors={1} bias_reg={2} reg_u={3} reg_i={4} reg_j={5} num_iter={6} learn_rate={7} bold_driver={8} fast_sampling_memory_limit={9} init_mean={10} init_stdev={11}",
								 this.GetType().Name, num_factors, BiasReg, reg_u, reg_i, reg_j, NumIter, learn_rate, BoldDriver, fast_sampling_memory_limit, InitMean, InitStdev);
		}
	}
}