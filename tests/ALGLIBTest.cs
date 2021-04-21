using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NUnit.Framework;

namespace ALGLIB
{
    /// <summary>
    /// Code to test the ALGLIB library
    /// See https://www.alglib.net/
    /// NuGet package: https://www.nuget.org/packages/alglib.net/
    /// About box and linearly constrained optimization (our interest here): https://www.alglib.net/optimization/boundandlinearlyconstrained.php
    /// minbleic sub package: https://www.alglib.net/translator/man/manual.csharp.html#unit_minbleic
    /// About optimization: http://www.alglib.net/optimization/
    /// </summary>
    [TestFixture]
    public class ALGLIBTest
    {
        [SetUp]
        public void Setup()
        {
        }

        public static void function1_grad(double[] x, ref double func, double[] grad, object obj)
        {
            // this callback calculates f(x0,x1) = 100*(x0+3)^4 + (x1-3)^4
            // and its derivatives df/d0 and df/dx1
            func = 100 * System.Math.Pow(x[0] + 3, 4) + System.Math.Pow(x[1] - 3, 4);
            grad[0] = 400 * System.Math.Pow(x[0] + 3, 3);
            grad[1] = 4 * System.Math.Pow(x[1] - 3, 3);
        }


        /// <summary>
        /// See https://www.alglib.net/translator/man/manual.csharp.html#example_minbleic_d_1
        /// </summary>
        [Test]
        public void minbleic_d_1_example()
        {
            //
            // This example demonstrates minimization of
            //
            //     f(x,y) = 100*(x+3)^4+(y-3)^4
            //
            // subject to box constraints
            //
            //     -1<=x<=+1, -1<=y<=+1
            //
            // using BLEIC optimizer with:
            // * initial point x=[0,0]
            // * unit scale being set for all variables (see minbleicsetscale for more info)
            // * stopping criteria set to "terminate after short enough step"
            // * OptGuard integrity check being used to check problem statement
            //   for some common errors like nonsmoothness or bad analytic gradient
            //
            // First, we create optimizer object and tune its properties:
            // * set box constraints
            // * set variable scales
            // * set stopping criteria
            //
            double[] x = new double[] {0, 0};
            double[] s = new double[] {1, 1};
            double[] bndl = new double[] {-1, -1};
            double[] bndu = new double[] {+1, +1};
            double epsg = 0;
            double epsf = 0;
            double epsx = 0.000001;
            int maxits = 0;
            alglib.minbleicstate state;
            alglib.minbleiccreate(x, out state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetscale(state, s);
            alglib.minbleicsetcond(state, epsg, epsf, epsx, maxits);

            //
            // Then we activate OptGuard integrity checking.
            //
            // OptGuard monitor helps to catch common coding and problem statement
            // issues, like:
            // * discontinuity of the target function (C0 continuity violation)
            // * nonsmoothness of the target function (C1 continuity violation)
            // * erroneous analytic gradient, i.e. one inconsistent with actual
            //   change in the target/constraints
            //
            // OptGuard is essential for early prototyping stages because such
            // problems often result in premature termination of the optimizer
            // which is really hard to distinguish from the correct termination.
            //
            // IMPORTANT: GRADIENT VERIFICATION IS PERFORMED BY MEANS OF NUMERICAL
            //            DIFFERENTIATION. DO NOT USE IT IN PRODUCTION CODE!!!!!!!
            //
            //            Other OptGuard checks add moderate overhead, but anyway
            //            it is better to turn them off when they are not needed.
            //
            alglib.minbleicoptguardsmoothness(state);
            alglib.minbleicoptguardgradient(state, 0.001);

            //
            // Optimize and evaluate results
            //
            alglib.minbleicreport rep;
            alglib.minbleicoptimize(state, function1_grad, null, null);
            alglib.minbleicresults(state, out x, out rep);

            Assert.AreEqual(4, rep.terminationtype);
            Assert.AreEqual(new double[] {-1.0, 1.0}, x);

            //
            // Check that OptGuard did not report errors
            //
            // NOTE: want to test OptGuard? Try breaking the gradient - say, add
            //       1.0 to some of its components.
            //
            alglib.optguardreport ogrep;
            alglib.minbleicoptguardresults(state, out ogrep);
            Assert.IsFalse(ogrep.badgradsuspected);
            Assert.IsFalse(ogrep.nonc0suspected);
            Assert.IsFalse(ogrep.nonc1suspected);
        }

        [Test]
        public void minbleic_d_2_example()
        {
            // This example demonstrates minimization of
            //
            //     f(x,y) = 100*(x+3)^4+(y-3)^4
            //
            // subject to inequality constraints
            //
            // * x>=2 (posed as general linear constraint),
            // * x+y>=6
            //
            // using BLEIC optimizer with
            // * initial point x=[0,0]
            // * unit scale being set for all variables (see minbleicsetscale for more info)
            // * stopping criteria set to "terminate after short enough step"
            // * OptGuard integrity check being used to check problem statement
            //   for some common errors like nonsmoothness or bad analytic gradient
            //
            // First, we create optimizer object and tune its properties:
            // * set linear constraints
            // * set variable scales
            // * set stopping criteria
            //
            double[] x = new double[] {5, 5};
            double[] s = new double[] {1, 1};
            double[,] c = new double[,] {{1, 0, 2}, {1, 1, 6}};
            int[] ct = new int[] {1, 1};
            alglib.minbleicstate state;
            double epsg = 0;
            double epsf = 0;
            double epsx = 0.000001;
            int maxits = 0;

            alglib.minbleiccreate(x, out state);
            alglib.minbleicsetlc(state, c, ct);
            alglib.minbleicsetscale(state, s);
            alglib.minbleicsetcond(state, epsg, epsf, epsx, maxits);

            //
            // Then we activate OptGuard integrity checking.
            //
            // OptGuard monitor helps to catch common coding and problem statement
            // issues, like:
            // * discontinuity of the target function (C0 continuity violation)
            // * nonsmoothness of the target function (C1 continuity violation)
            // * erroneous analytic gradient, i.e. one inconsistent with actual
            //   change in the target/constraints
            //
            // OptGuard is essential for early prototyping stages because such
            // problems often result in premature termination of the optimizer
            // which is really hard to distinguish from the correct termination.
            //
            // IMPORTANT: GRADIENT VERIFICATION IS PERFORMED BY MEANS OF NUMERICAL
            //            DIFFERENTIATION. DO NOT USE IT IN PRODUCTION CODE!!!!!!!
            //
            //            Other OptGuard checks add moderate overhead, but anyway
            //            it is better to turn them off when they are not needed.
            //
            alglib.minbleicoptguardsmoothness(state);
            alglib.minbleicoptguardgradient(state, 0.001);

            //
            // Optimize and evaluate results
            //
            alglib.minbleicreport rep;
            alglib.minbleicoptimize(state, function1_grad, null, null);
            alglib.minbleicresults(state, out x, out rep);

            Assert.AreEqual(4, rep.terminationtype);
            Assert.AreEqual(new double[] {2.0, 4.0}, x);

            //
            // Check that OptGuard did not report errors
            //
            // NOTE: want to test OptGuard? Try breaking the gradient - say, add
            //       1.0 to some of its components.
            //
            alglib.optguardreport ogrep;
            alglib.minbleicoptguardresults(state, out ogrep);
            Assert.IsFalse(ogrep.badgradsuspected);
            Assert.IsFalse(ogrep.nonc0suspected);
            Assert.IsFalse(ogrep.nonc1suspected);
        }

        public static void function1_func(double[] x, ref double func, object obj)
        {
            // this callback calculates f(x0,x1) = 100*(x0+3)^4 + (x1-3)^4
            func = 100 * System.Math.Pow(x[0] + 3, 4) + System.Math.Pow(x[1] - 3, 4);
        }

        /// <summary>
        /// See https://www.alglib.net/translator/man/manual.csharp.html#example_minbleic_numdiff
        /// </summary>
        [Test]
        public void minbleic_numdiff_example()
        {
            // This example demonstrates minimization of
            //
            //     f(x,y) = 100*(x+3)^4+(y-3)^4
            //
            // subject to box constraints
            //
            //     -1<=x<=+1, -1<=y<=+1
            //
            // using BLEIC optimizer with:
            // * numerical differentiation being used
            // * initial point x=[0,0]
            // * unit scale being set for all variables (see minbleicsetscale for more info)
            // * stopping criteria set to "terminate after short enough step"
            // * OptGuard integrity check being used to check problem statement
            //   for some common errors like nonsmoothness or bad analytic gradient
            //
            // First, we create optimizer object and tune its properties:
            // * set box constraints
            // * set variable scales
            // * set stopping criteria
            //
            double[] x = new double[] {0, 0};
            double[] s = new double[] {1, 1};
            double[] bndl = new double[] {-1, -1};
            double[] bndu = new double[] {+1, +1};
            alglib.minbleicstate state;
            double epsg = 0;
            double epsf = 0;
            double epsx = 0.000001;
            int maxits = 0;
            double diffstep = 1.0e-6;

            alglib.minbleiccreatef(x, diffstep, out state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetscale(state, s);
            alglib.minbleicsetcond(state, epsg, epsf, epsx, maxits);

            //
            // Then we activate OptGuard integrity checking.
            //
            // Numerical differentiation always produces "correct" gradient
            // (with some truncation error, but unbiased). Thus, we just have
            // to check smoothness properties of the target: C0 and C1 continuity.
            //
            // Sometimes user accidentally tries to solve nonsmooth problems
            // with smooth optimizer. OptGuard helps to detect such situations
            // early, at the prototyping stage.
            //
            alglib.minbleicoptguardsmoothness(state);

            //
            // Optimize and evaluate results
            //
            alglib.minbleicreport rep;
            alglib.minbleicoptimize(state, function1_func, null, null);
            alglib.minbleicresults(state, out x, out rep);

            Assert.AreEqual(4, rep.terminationtype);
            Assert.AreEqual(new double[] {-1.0, 1.0}, x);

            //
            // Check that OptGuard did not report errors
            //
            // Want to challenge OptGuard? Try to make your problem
            // nonsmooth by replacing 100*(x+3)^4 by 100*|x+3| and
            // re-run optimizer.
            //
            alglib.optguardreport ogrep;
            alglib.minbleicoptguardresults(state, out ogrep);
            Assert.IsFalse(ogrep.nonc0suspected);
            Assert.IsFalse(ogrep.nonc1suspected);
        }
    }
}
