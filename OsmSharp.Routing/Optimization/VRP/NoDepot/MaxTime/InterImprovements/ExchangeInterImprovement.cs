﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OsmSharp.Math.VRP.Core;
using OsmSharp.Math.VRP.Core.Routes;

namespace OsmSharp.Routing.Optimization.VRP.NoDepot.MaxTime.InterImprovements
{
    /// <summary>
    /// Applies inter-improvements by exchanging customers.
    /// </summary>
    public class ExchangeInterImprovement : IInterImprovement
    {
        /// <summary>
        /// Return the name of this improvement.
        /// </summary>
        public string Name
        {
            get
            {
                return "EX";
            }
        }

        /// <summary>
        /// Returns true if this operation is symmetric.
        /// </summary>
        public bool IsSymmetric
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Applies inter-improvements by exchanging customers.
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="solution"></param>
        /// <param name="route1_idx"></param>
        /// <param name="route2_idx"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public bool Improve(MaxTimeProblem problem, MaxTimeSolution solution,
            int route1_idx, int route2_idx, double max)
        {
            IRoute route1 = solution.Route(route1_idx);
            IRoute route2 = solution.Route(route2_idx);

            double total_before = problem.Time(solution.Route(route1_idx)) +
                problem.Time(solution.Route(route2_idx));

            double route1_size = solution[route1_idx];
            double route2_size = solution[route2_idx];

            // this heuristic removes a customer1 from route1 and a customer2 from route2 and inserts the customers again
            // but swappes them; customer1 in route2 and customer2 in route1.
            int previous_customer1 = -1;
            //if (route1.IsRound)
            //{
            //    previous_customer1= route1.Last; // set the previous customer.
            //} 
            foreach (int customer1 in route1)
            { // loop over all customers in route1.
                if (previous_customer1 >= 0)
                { // the previous customer is set.
                    int next_customer1 = route1.GetNeigbours(customer1)[0];
                    int previous_customer2 = -1;
                    //if (route2.IsRound)
                    //{
                    //    previous_customer2 = route2.Last; // set the previous customer.
                    //}

                    foreach (int customer2 in route2)
                    { // loop over all customers in route2. 
                        int next_customer2 = route2.GetNeigbours(customer2)[0];
                        if (previous_customer2 >= 0)
                        { // the previous customer is set.
                            float weight1 = (float)problem.WeightMatrix[previous_customer1][customer1] +
                                (float)problem.WeightMatrix[customer1][next_customer1];
                            float weight2 = (float)problem.WeightMatrix[previous_customer2][customer2] +
                                (float)problem.WeightMatrix[customer2][next_customer2];

                            float weight1_after = (float)problem.WeightMatrix[previous_customer1][customer2] +
                                (float)problem.WeightMatrix[customer2][next_customer1];
                            float weight2_after = (float)problem.WeightMatrix[previous_customer2][customer1] +
                                (float)problem.WeightMatrix[customer1][next_customer2];
                            double difference = (weight1_after + weight2_after) - (weight1 + weight2);

                            if (difference < -0.01)
                            { // the old weights are bigger!
                                // check if the new routes are bigger than max.
                                if (route1_size + (weight1_after - weight1) <= max &&
                                    route2_size + (weight2_after - weight1) <= max)
                                { // the exchange can happen, both routes stay within bound!
                                    // exchange customer.
                                    int count_before = route1.Count + route2.Count;

                                    //route1.Remove(customer1);
                                    //route2.Remove(customer2);
                                    if (previous_customer1 == next_customer2)
                                    {
                                        throw new Exception();
                                    }

                                    //route1.InsertAfter(previous_customer1, customer2);
                                    //route2.InsertAfter(previous_customer2, customer1);
                                    route1.ReplaceEdgeFrom(previous_customer1, customer2);
                                    route1.ReplaceEdgeFrom(customer2, next_customer1);
                                    route2.ReplaceEdgeFrom(previous_customer2, customer1);
                                    route2.ReplaceEdgeFrom(customer1, next_customer2);

                                    int count_after = route1.Count + route2.Count;
                                    if (count_before != count_after)
                                    {
                                        throw new Exception();
                                    }

                                    double total_after = problem.Time(solution.Route(route1_idx)) +
                                        problem.Time(solution.Route(route2_idx));
                                    if (total_after >= total_before)
                                    {
                                        throw new Exception("this is not an improvement!");
                                    }

                                    return true;
                                }
                            }
                        }

                        previous_customer2 = customer2; // set the previous customer.
                    }
                }
                previous_customer1 = customer1; // set the previous customer.
            }
            return false;
        }
    }
}
