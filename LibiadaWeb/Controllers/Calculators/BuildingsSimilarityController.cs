﻿namespace LibiadaWeb.Controllers.Calculators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using LibiadaCore.Core;
    using LibiadaCore.Misc.Iterators;

    using LibiadaWeb.Helpers;
    using LibiadaWeb.Models.Repositories.Sequences;
    using Models;

    using Newtonsoft.Json;

    /// <summary>
    /// The buildings comparison controller.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class BuildingsSimilarityController : AbstractResultController
    {
        /// <summary>
        /// The db.
        /// </summary>
        private readonly LibiadaWebEntities db;

        /// <summary>
        /// The sequence repository.
        /// </summary>
        private readonly CommonSequenceRepository sequenceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingsSimilarityController"/> class.
        /// </summary>
        public BuildingsSimilarityController() : base("Buildings comparison")
        {
            db = new LibiadaWebEntities();
            sequenceRepository = new CommonSequenceRepository(db);
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult Index()
        {
            var viewDataHelper = new ViewDataHelper(db);
            ViewBag.data = JsonConvert.SerializeObject(viewDataHelper.FillViewData(2, 2, true, "Compare"));
            ViewBag.angularController = "BuildingsSimilarityController";
            return View();
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <param name="matterIds">
        /// The matter ids.
        /// </param>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <param name="congeneric">
        /// The congeneric.
        /// </param>
        /// <param name="notationId">
        /// The notation id.
        /// </param>
        /// <param name="languageId">
        /// The language id.
        /// </param>
        /// <param name="translatorId">
        /// The translator id.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if count of matters is not 2.
        /// </exception>
        [HttpPost]
        public ActionResult Index(long[] matterIds, int length, bool congeneric, int notationId, int? languageId, int? translatorId)
        {
            return Action(() =>
            {
                if (matterIds.Length != 2)
                {
                    throw new ArgumentException("Count of selected matters must be 2.", "matterIds");
                }

                var firstMatterId = matterIds[0];
                var secondMatterId = matterIds[1];

                long firstSequenceId;
                if (db.Matter.Single(m => m.Id == firstMatterId).NatureId == Aliases.Nature.Literature)
                {
                    firstSequenceId = db.LiteratureSequence.Single(l => l.MatterId == firstMatterId &&
                                l.NotationId == notationId
                                && l.LanguageId == languageId
                                && ((translatorId == null && l.TranslatorId == null)
                                                || (translatorId == l.TranslatorId))).Id;
                }
                else
                {
                    firstSequenceId = db.CommonSequence.Single(c => c.MatterId == firstMatterId && c.NotationId == notationId).Id;
                }

                Chain firstLibiadaChain = sequenceRepository.ToLibiadaChain(firstSequenceId);

                long secondSequenceId;
                if (db.Matter.Single(m => m.Id == firstMatterId).NatureId == Aliases.Nature.Literature)
                {
                    secondSequenceId = db.LiteratureSequence.Single(l => l.MatterId == firstMatterId &&
                                l.NotationId == notationId
                                && l.LanguageId == languageId
                                && ((translatorId == null && l.TranslatorId == null)
                                                || (translatorId == l.TranslatorId))).Id;
                }
                else
                {
                    secondSequenceId = db.CommonSequence.Single(c => c.MatterId == firstMatterId && c.NotationId == notationId).Id;
                }

                Chain secondLibiadaChain = sequenceRepository.ToLibiadaChain(secondSequenceId);

                AbstractChain res1 = null;
                AbstractChain res2 = null;

                int i = 0;
                int j = 0;
                var firstIterator = new IteratorStart(firstLibiadaChain, length, 1);
                bool duplicate = false;
                while (!duplicate && firstIterator.Next())
                {
                    i++;
                    var firstTempChain = (Chain)firstIterator.Current();
                    var secondIterator = new IteratorStart(secondLibiadaChain, length, 1);
                    j = 0;
                    while (!duplicate && secondIterator.Next())
                    {
                        j++;
                        var secondTempChain = (Chain)secondIterator.Current();

                        if (congeneric)
                        {
                            for (int a = 0; a < firstTempChain.Alphabet.Cardinality; a++)
                            {
                                CongenericChain firstChain = firstTempChain.CongenericChain(a);
                                for (int b = 0; b < secondTempChain.Alphabet.Cardinality; b++)
                                {
                                    CongenericChain secondChain = secondTempChain.CongenericChain(b);
                                    if (!firstChain.Equals(secondChain) && CompareBuildings(firstChain.Building, secondChain.Building))
                                    {
                                        res1 = firstChain;
                                        res2 = secondChain;
                                        duplicate = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!firstTempChain.Equals(secondTempChain) &&
                                CompareBuildings(secondTempChain.Building, firstTempChain.Building))
                            {
                                res1 = firstTempChain;
                                res2 = secondTempChain;
                                duplicate = true;
                            }
                        }
                    }
                }

                return new Dictionary<string, object>
                {
                    { "duplicate", duplicate },
                    { "firstSequenceName", db.Matter.Single(m => m.Id == firstMatterId).Name },
                    { "secondSequenceName", db.Matter.Single(m => m.Id == secondMatterId).Name },
                    { "res1", res1 },
                    { "res2", res2 },
                    { "pos1", i },
                    { "pos2", j }
                };
            });
        }

        /// <summary>
        /// The compare buildings.
        /// </summary>
        /// <param name="firstBuilding">
        /// The first building.
        /// </param>
        /// <param name="secondBuilding">
        /// The second building.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool CompareBuildings(int[] firstBuilding, int[] secondBuilding)
        {
            if (firstBuilding.Length != secondBuilding.Length)
            {
                return false;
            }

            return firstBuilding.SequenceEqual(secondBuilding);
        }
    }
}