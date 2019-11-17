﻿using System;

namespace LibiadaWeb.Controllers.Calculators
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.Mvc.Html;
    using LibiadaCore.Core;
    using LibiadaCore.Core.Characteristics.Calculators.FullCalculators;
    using LibiadaCore.Extensions;
    using LibiadaWeb.Helpers;
    using LibiadaWeb.Models.CalculatorsData;
    using LibiadaWeb.Models.Repositories.Catalogs;
    using LibiadaWeb.Tasks;

    using Newtonsoft.Json;

    using SequenceGenerator;

    /// <summary>
    /// Calculates accordance of orders by intervals distributions.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class IntervalsCharacteristicsDistributionController : AbstractResultController
    {
        /// <summary>
        /// The characteristic type link repository.
        /// </summary>
        private readonly FullCharacteristicRepository characteristicTypeLinkRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalsCharacteristicsDistributionController"/> class.
        /// </summary>
        public IntervalsCharacteristicsDistributionController() : base(TaskType.IntervalsCharacteristicsDistribution)
        {
            characteristicTypeLinkRepository = FullCharacteristicRepository.Instance;
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult Index()
        {
            var imageTransformers = EnumHelper.GetSelectList(typeof(ImageTransformer));

            using (var db = new LibiadaWebEntities())
            {
                var viewDataHelper = new ViewDataHelper(db);
                Dictionary<string, object> viewData = viewDataHelper.GetCharacteristicsData(CharacteristicCategory.Full);
                ViewBag.data = JsonConvert.SerializeObject(viewData);
                return View();
            }
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <param name="alphabetCardinality">
        /// The alphabet cardinality.
        /// </param>
        /// <param name="generateStrict">
        /// The generate strict.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        public ActionResult Index(int length, int alphabetCardinality, int generateStrict, short[] characteristicLinkIds)
        {
            return CreateTask(() =>
            {
                var orderGenerator = new OrderGenerator();
                var orders = new List<int[]>();
                switch (generateStrict)
                {
                    case 0:
                        orders = orderGenerator.StrictGenerateOrders(length, alphabetCardinality);
                        break;
                    case 1:
                        orders = orderGenerator.GenerateOrders(length, alphabetCardinality);
                        break;
                    default: throw new ArgumentException("Invalid type of generate");
                }

                var characteristics = new double[orders.Count][];
                var sequencesCharacteristics = new List<SequenceCharacteristics>();
                for (int i = 0; i < orders.Count; i++)
                {
                    sequencesCharacteristics.Add(new SequenceCharacteristics());
                }
                for (int j = 0; j < orders.Count; j++)
                {
                    var sequence = new Chain(orders[j]);
                    sequence.FillIntervalManagers();
                    characteristics[j] = new double[characteristicLinkIds.Length];
                    for (int k = 0; k < characteristicLinkIds.Length; k++)
                    {

                        Link characteristicLink = characteristicTypeLinkRepository.GetLinkForCharacteristic(characteristicLinkIds[k]);
                        FullCharacteristic characteristic = characteristicTypeLinkRepository.GetCharacteristic(characteristicLinkIds[k]);
                        IFullCalculator calculator = FullCalculatorsFactory.CreateCalculator(characteristic);

                        characteristics[j][k] = calculator.Calculate(sequence, characteristicLink);
                    }

                    sequencesCharacteristics[j] = new SequenceCharacteristics
                    {
                        MatterName = String.Join(",", orders[j].Select(n => n.ToString()).ToArray()),
                        Characteristics = characteristics[j]
                    };
                }
                sequencesCharacteristics.RemoveAll(el => el.Characteristics.Any(v => Double.IsInfinity(v) ||
                                                                                           Double.IsNaN(v) ||
                                                                                           Double.IsNegativeInfinity(v) ||
                                                                                           Double.IsPositiveInfinity(v)));



                var characteristicNames = new string[characteristicLinkIds.Length];
                var characteristicsList = new SelectListItem[characteristicLinkIds.Length];
                for (int k = 0; k < characteristicLinkIds.Length; k++)
                {
                    characteristicNames[k] = characteristicTypeLinkRepository.GetCharacteristicName(characteristicLinkIds[k]);
                    characteristicsList[k] = new SelectListItem
                    {
                        Value = k.ToString(),
                        Text = characteristicNames[k],
                        Selected = false
                    };
                }
                var index = new int[characteristicsList.Length];
                for (int i = 0; i < index.Length; i++)
                {
                    index[i] = i;
                }

                var resultIntervals = new Dictionary<string, Dictionary<IntervalsDistribution, Dictionary<int[], SequenceCharacteristics>>>();
                foreach (var link in EnumExtensions.ToArray<Link>())
                {
                    if (link == Link.NotApplied)
                    {
                        continue;
                    }
                    var accordance = IntervalsDistributionExtractor.GetOrdersIntervalsDistributionsAccordance(orders.ToArray(), link);
                    var resultAccordance = new Dictionary<IntervalsDistribution, Dictionary<int[], SequenceCharacteristics>>();
                    foreach(var element in accordance)
                    {
                        resultAccordance.Add(element.Key, new Dictionary<int[], SequenceCharacteristics>());
                        foreach(var order in element.Value)
                        {
                            resultAccordance[element.Key].Add(order, sequencesCharacteristics.FirstOrDefault(el => el.MatterName.SequenceEqual(String.Join(",", order.Select(n => n.ToString()).ToArray()))));
                        }
                    }
                    resultIntervals.Add(EnumExtensions.GetDisplayValue<Link>(link), resultAccordance);
                }


                var list = EnumHelper.GetSelectList(typeof(Link));
                list.RemoveAt(0);
                var data = new Dictionary<string, object>
                {
                    { "result", resultIntervals.Select(r => new
                    {
                        link = r.Key.ToString(),
                        accordance = r.Value.Select(d => new {
                            distributionIntervals = d.Key.Distribution.Select(pair => new
                            {
                                interval = pair.Key,
                                count = pair.Value
                            }).ToArray(),
                            orders = d.Value.Select(o => new
                            {
                                order = o.Key,
                                characteristics = o.Value
                            })
                        })
                    })
                    },
                    {"linkList",list },
                    { "characteristicNames", characteristicNames },
                    { "characteristicsList", characteristicsList },
                    { "characteristicsIndex", index }
                };

                return new Dictionary<string, object>
                {
                    { "data", JsonConvert.SerializeObject(data) }
                };
            });
        }
    }
}