﻿namespace LibiadaWeb.Controllers.Calculators
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Web.Mvc;

    using LibiadaCore.Extensions;

    using LibiadaWeb.Helpers;
    using LibiadaWeb.Models.Calculators;
    using LibiadaWeb.Models.CalculatorsData;
    using LibiadaWeb.Models.Repositories.Catalogs;
    using LibiadaWeb.Tasks;

    using Models.Repositories.Sequences;

    using Newtonsoft.Json;

    /// <summary>
    /// The subsequences comparer controller.
    /// </summary>
    [Authorize]
    public class SubsequencesComparerController : AbstractResultController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubsequencesComparerController"/> class.
        /// </summary>
        public SubsequencesComparerController() : base(TaskType.SubsequencesComparer)
        {
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult Index()
        {
            using (var db = new LibiadaWebEntities())
            {
                var viewDataHelper = new ViewDataHelper(db);
                ViewBag.data = JsonConvert.SerializeObject(viewDataHelper.FillSubsequencesViewData(2, int.MaxValue, "Compare"));
            }

            return View();
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <param name="matterIds">
        /// The matter ids.
        /// </param>
        /// <param name="characteristicLinkId">
        /// The characteristic type and link id.
        /// </param>
        /// <param name="subsequencesCharacteristicLinkId">
        /// The subsequences characteristic type link id.
        /// </param>
        /// <param name="features">
        /// The feature ids.
        /// </param>
        /// <param name="maxPercentageDifference">
        /// The precision.
        /// </param>
        /// <param name="filters">
        /// Filters for the subsequences.
        /// Filters are applied in "OR" logic (if subsequence corresponds to any filter it is added to calculation).
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if count of matters is not 2.
        /// </exception>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(
            long[] matterIds,
            short characteristicLinkId,
            short subsequencesCharacteristicLinkId,
            Feature[] features,
            string maxPercentageDifference,
            string[] filters)
        {
            return CreateTask(() =>
            {
                var attributeValues = new List<AttributeValue>();
                var characteristics = new SubsequenceData[matterIds.Length][];
                string characteristicName;

                long[] parentSequenceIds;
                var matterNames = new string[matterIds.Length];

                string sequenceCharacteristicName;

                int mattersCount = matterIds.Length;
                var subsequencesCount = new int[mattersCount];
                List<CharacteristicData> localCharacteristicsType;

                using (var db = new LibiadaWebEntities())
                {
                    // Sequences characteristic
                    var geneticSequenceRepository = new GeneticSequenceRepository(db);
                    long[] chains = geneticSequenceRepository.GetNucleotideSequenceIds(matterIds);

                    var sequencesCharacteristicTypeLinkRepository = FullCharacteristicRepository.Instance;
                    sequenceCharacteristicName = sequencesCharacteristicTypeLinkRepository.GetCharacteristicName(characteristicLinkId);

                    // Sequences characteristic
                    double[] completeGenomesCharacteristics = SequencesCharacteristicsCalculator.Calculate(chains, characteristicLinkId);

                    var matterCharacteristics = new KeyValuePair<long, double>[matterIds.Length];

                    for (int i = 0; i < completeGenomesCharacteristics.Length; i++)
                    {
                        matterCharacteristics[i] = new KeyValuePair<long, double>(matterIds[i], completeGenomesCharacteristics[i]);
                    }

                    matterIds = matterCharacteristics.OrderBy(mc => mc.Value).Select(mc => mc.Key).ToArray();

                    // Subsequences characteristics
                    var parentSequences = db.DnaSequence.Include(s => s.Matter)
                                            .Where(s => s.Notation == Notation.Nucleotides && matterIds.Contains(s.MatterId))
                                            .Select(s => new { s.Id, s.MatterId, MatterName = s.Matter.Name })
                                            .ToDictionary(s => s.Id);
                    parentSequenceIds = parentSequences.OrderBy(ps => Array.IndexOf(matterIds, ps.Value.MatterId)).Select(ps => ps.Key).ToArray();

                    for (int n = 0; n < parentSequenceIds.Length; n++)
                    {
                        matterNames[n] = parentSequences[parentSequenceIds[n]].MatterName;
                    }

                    var subsequencesCharacteristicTypeLinkRepository = FullCharacteristicRepository.Instance;

                    characteristicName = subsequencesCharacteristicTypeLinkRepository.GetCharacteristicName(subsequencesCharacteristicLinkId);

                    var fullCharacteristicRepository = FullCharacteristicRepository.Instance;
                    localCharacteristicsType = fullCharacteristicRepository.GetCharacteristicTypes();
                }

                var characteristicsValues = new Dictionary<double, List<(int, int)>>();

                // cycle through matters
                for (int i = 0; i < mattersCount; i++)
                {
                    var subsequencesData = SubsequencesCharacteristicsCalculator.CalculateSubsequencesCharacteristics(
                            new[] { subsequencesCharacteristicLinkId },
                            features,
                            parentSequenceIds[i],
                            attributeValues,
                            filters);

                    subsequencesCount[i] = subsequencesData.Length;

                    characteristics[i] = subsequencesData;

                    for (int j = 0; j < subsequencesData.Length; j++)
                    {
                        SubsequenceData value = subsequencesData[j];
                        if (characteristicsValues.TryGetValue(value.CharacteristicsValues[0], out List<(int, int)> subsequencesList))
                        {
                            subsequencesList.Add((i, j));
                        }
                        else
                        {
                            characteristicsValues.Add(value.CharacteristicsValues[0], new List<(int, int)> { (i, j) });
                        }
                    }
                }

                var orderedCharacteristicsValues = characteristicsValues.OrderBy(c => c.Key).ToList();
                var allSimilarPairs = new List<((int, int),(int, int))>();
                for (int i = 0; i < orderedCharacteristicsValues.Count; i++)
                {
                    allSimilarPairs.AddRange(ExtractAllPossiblePairs(orderedCharacteristicsValues[i].Value));
                }

                var similarities = new object[mattersCount, mattersCount];
                var equalElements = new List<(int, int)>[mattersCount, mattersCount];
                for (int i = 0; i < mattersCount; i++)
                {
                    for (int j = 0; j < mattersCount; j++)
                    {
                        equalElements[i,j] = new List<(int, int)>();
                    }
                }

                foreach (((int firstMatter, int firstSubsequence),(int secondMatter, int secondSubsequence)) in allSimilarPairs)
                {
                    equalElements[firstMatter, secondMatter].Add((firstSubsequence, secondSubsequence));
                    equalElements[secondMatter, firstMatter].Add((secondSubsequence, firstSubsequence));
                }

                for (int i = 0; i < mattersCount; i++)
                {
                    for (int j = 0; j < mattersCount; j++)
                    {
                        int firstEqualCount = equalElements[i, j].Select(s => s.Item1).Distinct().Count();
                        int secondEqualCount = equalElements[i, j].Select(s => s.Item2).Distinct().Count();

                        double differenceFinal = firstEqualCount < secondEqualCount ? firstEqualCount * 2d : secondEqualCount * 2d;
                        double formula1 = differenceFinal / (subsequencesCount[i] + subsequencesCount[j]);

                        double formula2 = 0;
                        //if (equalElements[i, j].Count != 0 && formula1 != 0)
                        //{
                        //    formula2 = (differenceSum / equalElements[i, j].Count) / formula1;
                        //}

                        double firstCharacteristicSum = equalElements[i, j]
                                                            .Select(s => s.Item1)
                                                            .Distinct()
                                                            .Sum(s => characteristics[i][s].CharacteristicsValues[0]);

                        double secondCharacteristicSum = equalElements[i, j]
                                                            .Select(s => s.Item2)
                                                            .Distinct()
                                                            .Sum(s => characteristics[j][s].CharacteristicsValues[0]);

                        double similarSequencesCharacteristicValue = equalElements[i, j].Select(s => s.Item1).Distinct().Sum(s => characteristics[i][s].CharacteristicsValues[0]) < secondCharacteristicSum ?
                                                                         firstCharacteristicSum * 2d : secondCharacteristicSum * 2d;


                        double formula3 = similarSequencesCharacteristicValue / (characteristics[i].Sum(c => c.CharacteristicsValues[0]) + characteristics[j].Sum(c => c.CharacteristicsValues[0]));


                        const int digits = 5;
                        similarities[i, j] = new
                        {
                            formula1 = Math.Round(formula1, digits),
                            formula2 = Math.Round(formula2, digits),
                            formula3 = Math.Round(formula3, digits),
                            firstAbsolutelyEqualElementsCount = firstEqualCount,
                            firstNearlyEqualElementsCount = 0,
                            firstNotEqualElementsCount = characteristics[i].Length - firstEqualCount,

                            secondAbsolutelyEqualElementsCount = secondEqualCount,
                            secondNearlyEqualElementsCount = 0,
                            secondNotEqualElementsCount = characteristics[j].Length - secondEqualCount,
                        };
                    }
                }

                //double decimalDifference = double.Parse(maxPercentageDifference, CultureInfo.InvariantCulture) / 100;



                //for (int i = 0; i < characteristics.Length; i++)
                //{
                //    for (int j = 0; j < characteristics.Length; j++)
                //    {
                //        int firstAbsolutelyEqualElementsCount = 0;
                //        int firstNearlyEqualElementsCount = 0;

                //        int secondAbsolutelyEqualElementsCount = 0;
                //        int secondNearlyEqualElementsCount = 0;

                //        equalElements[i, j] = new List<SubsequenceComparisonData>();
                //        double similarSequencesCharacteristicValueFirst = 0;
                //        var similarSequencesCharacteristicValueSecond = new Dictionary<int, double>();

                //        int secondArrayStartPosition = 0;
                //        double differenceSum = 0;

                //        int equalElementsCountFromFirst = 0;
                //        var equalElementsCountFromSecond = new Dictionary<int, bool>();

                //        int equalPairsCount = 0;
                //        double difference = 0;

                //        for (int k = 0; k < characteristics[i].Length; k++)
                //        {
                //            bool? equalFoundFromFirstAbsolutely = null;
                //            bool? equalFoundFromSecondAbsolutely = null;

                //            double first = characteristics[i][k].CharacteristicsValues[0];

                //            for (int l = secondArrayStartPosition; l < characteristics[j].Length; l++)
                //            {
                //                double second = characteristics[j][l].CharacteristicsValues[0];

                //                difference = CalculateAverageDifference(first, second);

                //                if (difference <= decimalDifference)
                //                {
                //                    if (!equalFoundFromFirstAbsolutely.HasValue || !equalFoundFromFirstAbsolutely.Value)
                //                    {
                //                        equalFoundFromFirstAbsolutely = difference == 0;
                //                    }

                //                    equalPairsCount++;

                //                    if (!equalElementsCountFromSecond.ContainsKey(l))
                //                    {
                //                        equalElementsCountFromSecond.Add(l, difference == 0);
                //                        differenceSum += difference;
                //                    }
                //                    else
                //                    {
                //                        if (!equalElementsCountFromSecond[l])
                //                        {
                //                            equalElementsCountFromSecond[l] = difference == 0;
                //                        }
                //                    }

                //                    if (!similarSequencesCharacteristicValueSecond.ContainsKey(l))
                //                    {
                //                        similarSequencesCharacteristicValueSecond.Add(l, second);
                //                    }

                //                    if (i != j)
                //                    {
                //                        equalElements[i, j].Add(new SubsequenceComparisonData
                //                        {
                //                            Difference = difference,
                //                            FirstSubsequenceIndex = k,
                //                            SecondSubsequenceIndex = l
                //                        });
                //                    }

                //                    if (l < characteristics[j].Length - 1)
                //                    {
                //                        bool nextElementInSecondArrayIsEqual = CalculateAverageDifference(second, characteristics[j][l + 1].CharacteristicsValues[0]) <= decimalDifference;

                //                        if (!nextElementInSecondArrayIsEqual)
                //                        {
                //                            break;
                //                        }
                //                    }
                //                }
                //                else if (second < first)
                //                {
                //                    secondArrayStartPosition++;
                //                }
                //            }

                //            if (equalFoundFromFirstAbsolutely.HasValue)
                //            {
                //                equalElementsCountFromFirst++;
                //                similarSequencesCharacteristicValueFirst += first;

                //                // fill equal elements count for first chain
                //                if (equalFoundFromFirstAbsolutely.Value)
                //                {
                //                    firstAbsolutelyEqualElementsCount++;
                //                }
                //                else
                //                {
                //                    firstNearlyEqualElementsCount++;
                //                }
                //            }
                //        }

                //    secondAbsolutelyEqualElementsCount = equalElementsCountFromSecond.Count(e => e.Value);
                //    secondNearlyEqualElementsCount = equalElementsCountFromSecond.Count(e => !e.Value);

                //    double differenceSecondFinal = equalElementsCountFromSecond.Count;
                //    double differenceFinal = equalElementsCountFromFirst < differenceSecondFinal ? equalElementsCountFromFirst * 2d : differenceSecondFinal * 2d;

                //    double formula1 = differenceFinal / (subsequencesCount[i] + subsequencesCount[j]);

                //    double formula2 = 0;
                //    if (equalPairsCount != 0 && formula1 != 0)
                //    {
                //        formula2 = (differenceSum / equalPairsCount) / formula1;
                //    }

                //    double similarSequencesCharacteristicValueSecondFinal = similarSequencesCharacteristicValueSecond.Sum(s => s.Value);
                //    double similarSequencesCharacteristicValue = similarSequencesCharacteristicValueFirst < similarSequencesCharacteristicValueSecondFinal ?
                //                        similarSequencesCharacteristicValueFirst * 2d : similarSequencesCharacteristicValueSecondFinal * 2d;

                //    double formula3 = similarSequencesCharacteristicValue / (characteristics[i].Sum(c => c.CharacteristicsValues[0]) + characteristics[j].Sum(c => c.CharacteristicsValues[0]));

                //    const int digits = 5;

                //    similarities[i, j] = new
                //    {
                //        formula1 = Math.Round(formula1, digits),
                //        formula2 = Math.Round(formula2, digits),
                //        formula3 = Math.Round(formula3, digits),

                //        firstAbsolutelyEqualElementsCount,
                //        firstNearlyEqualElementsCount,
                //        firstNotEqualElementsCount = characteristics[i].Length - (firstAbsolutelyEqualElementsCount + firstNearlyEqualElementsCount),

                //        secondAbsolutelyEqualElementsCount,
                //        secondNearlyEqualElementsCount,
                //        secondNotEqualElementsCount = characteristics[j].Length - (secondAbsolutelyEqualElementsCount + secondNearlyEqualElementsCount),
                //    };

                //    equalElements[i, j] = equalElements[i, j].OrderBy(e => e.Difference).ToList();
                //}
                //}

                var result = new Dictionary<string, object>
                {
                    { "mattersNames", matterNames },
                    { "characteristicName", characteristicName },
                    { "similarities", similarities },
                    { "characteristics", characteristics },
                    { "features", features.ToDictionary(f => (byte)f, f => f.GetDisplayValue()) },
                    { "attributeValues", attributeValues.Select(sa => new { attribute = sa.AttributeId, value = sa.Value }) },
                    { "attributes", EnumExtensions.ToArray<LibiadaWeb.Attribute>().ToDictionary(a => (byte)a, a => a.GetDisplayValue()) },
                    { "maxPercentageDifference", maxPercentageDifference },
                    { "sequenceCharacteristicName", sequenceCharacteristicName },
                    { "characteristicTypes", localCharacteristicsType }
                };

                return new Dictionary<string, object>
                           {
                               { "additionalData", equalElements },
                               { "data", JsonConvert.SerializeObject(result) }
                           };
            });
        }

        /// <summary>
        /// Calculates average difference.
        /// </summary>
        /// <param name="first">
        /// The first.
        /// </param>
        /// <param name="second">
        /// The second.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        private double CalculateAverageDifference(double first, double second)
        {
            return Math.Abs((first - second) / ((first + second) / 2));
        }

        /// <summary>
        /// Extract all possible pairs from given list.
        /// (calculates Cartesian product)
        /// </summary>
        /// <param name="list">
        /// The list for pairs extraction.
        /// </param>
        /// <returns>
        /// The <see cref="T:List{((int,int), (int,int))}"/>.
        /// </returns>
        private List<((int, int), (int, int))> ExtractAllPossiblePairs(List<(int, int)> list)
        {
            var result = new List<((int, int),(int, int))> ();
            if (list.Count < 2)
            {
                return result;
            }

            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    result.Add((list[i], list[j]));
                }
            }

            return result;
        }
    }
}
