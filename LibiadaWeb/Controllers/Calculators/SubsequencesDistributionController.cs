﻿namespace LibiadaWeb.Controllers.Calculators
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using LibiadaCore.Core;
    using LibiadaCore.Core.Characteristics;
    using LibiadaCore.Core.Characteristics.Calculators;

    using LibiadaWeb.Helpers;
    using LibiadaWeb.Models;
    using LibiadaWeb.Models.Calculators;
    using LibiadaWeb.Models.Repositories.Catalogs;
    using LibiadaWeb.Models.Repositories.Sequences;

    using Newtonsoft.Json;

    /// <summary>
    /// The subsequences distribution controller.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class SubsequencesDistributionController : AbstractResultController
    {
        /// <summary>
        /// The db.
        /// </summary>
        private readonly LibiadaWebEntities db;

        /// <summary>
        /// The sequence repository.
        /// </summary>
        private readonly CommonSequenceRepository commonSequenceRepository;

        /// <summary>
        /// The subsequence extractor.
        /// </summary>
        private readonly SubsequenceExtractor subsequenceExtractor;

        /// <summary>
        /// The characteristic type repository.
        /// </summary>
        private readonly CharacteristicTypeLinkRepository characteristicTypeLinkRepository;

        /// <summary>
        /// The sequence attribute repository.
        /// </summary>
        private readonly SequenceAttributeRepository sequenceAttributeRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubsequencesDistributionController"/> class.
        /// </summary>
        public SubsequencesDistributionController() : base("SubsequencesDistribution", "Subsequences distribution")
        {
            db = new LibiadaWebEntities();
            commonSequenceRepository = new CommonSequenceRepository(db);
            subsequenceExtractor = new SubsequenceExtractor(db);
            characteristicTypeLinkRepository = new CharacteristicTypeLinkRepository(db);
            sequenceAttributeRepository = new SequenceAttributeRepository(db);
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
            ViewBag.data = JsonConvert.SerializeObject(viewDataHelper.GetSubsequencesViewData(1, int.MaxValue, true, "Calculate"));
            ViewBag.angularController = "SubsequencesDistributionController";
            return View();
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <param name="matterIds">
        /// The matter ids.
        /// </param>
        /// <param name="firstCharacteristicTypeLinkId">
        /// The first characteristic type and link id.
        /// </param>
        /// <param name="firstNotationId">
        /// The first notation id.
        /// </param>
        /// <param name="secondCharacteristicTypeLinkId">
        /// The second characteristic type and link id.
        /// </param>
        /// <param name="secondNotationId">
        /// The second notation id.
        /// </param>
        /// <param name="featureIds">
        /// The feature ids.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        public ActionResult Index(
            long[] matterIds,
            int firstCharacteristicTypeLinkId,
            int firstNotationId,
            int secondCharacteristicTypeLinkId,
            int secondNotationId,
            int[] featureIds)
        {
            return Action(() =>
            {
                var sequenceCharacterisitcs = new List<SequenceCharacteristics>();

                var sequenceIds = db.DnaSequence.Where(c => matterIds.Contains(c.MatterId) && c.NotationId == secondNotationId).Select(c => c.Id).ToList();

                int maxSubsequences = 0;
                int maxSubsequencesIndex = 0;

                for (int w = 0; w < matterIds.Length; w++)
                {
                    long matterId = matterIds[w];
                    var matterName = db.Matter.Single(m => m.Id == matterId).Name;

                    long sequenceId = db.CommonSequence.Single(c => c.MatterId == matterId && c.NotationId == firstNotationId).Id;

                    double sequenceCharacteristic;

                    if (db.Characteristic.Any(c => c.SequenceId == sequenceId && c.CharacteristicTypeLinkId == firstCharacteristicTypeLinkId))
                    {
                        sequenceCharacteristic = db.Characteristic.Single(c => c.SequenceId == sequenceId && c.CharacteristicTypeLinkId == firstCharacteristicTypeLinkId).Value;
                    }
                    else
                    {
                        Chain tempChain = commonSequenceRepository.ToLibiadaChain(sequenceId);
                        tempChain.FillIntervalManagers();
                        string fullClassName = characteristicTypeLinkRepository.GetCharacteristicType(firstCharacteristicTypeLinkId).ClassName;
                        IFullCalculator fullCalculator = CalculatorsFactory.CreateFullCalculator(fullClassName);
                        var fullLink = characteristicTypeLinkRepository.GetLibiadaLink(firstCharacteristicTypeLinkId);
                        sequenceCharacteristic = fullCalculator.Calculate(tempChain, fullLink);

                        var dataBaseCharacteristic = new Characteristic
                        {
                            SequenceId = sequenceId,
                            CharacteristicTypeLinkId = firstCharacteristicTypeLinkId,
                            Value = sequenceCharacteristic,
                            ValueString = sequenceCharacteristic.ToString()
                        };
                        db.Characteristic.Add(dataBaseCharacteristic);
                        db.SaveChanges();
                    }

                    List<Subsequence> subsequences = subsequenceExtractor.GetSubsequences(sequenceIds[w], featureIds);
                    var sequences = subsequenceExtractor.ExtractChains(subsequences, sequenceIds[w]);

                    if (maxSubsequences < subsequences.Count)
                    {
                        maxSubsequences = subsequences.Count;
                        maxSubsequencesIndex = w;
                    }

                    var subsequencesCharacteristics = new List<SubsequenceCharacteristic>();
                    string className = characteristicTypeLinkRepository.GetCharacteristicType(secondCharacteristicTypeLinkId).ClassName;
                    IFullCalculator calculator = CalculatorsFactory.CreateFullCalculator(className);
                    var link = characteristicTypeLinkRepository.GetLibiadaLink(secondCharacteristicTypeLinkId);

                    for (int j = 0; j < sequences.Count; j++)
                    {
                        long subsequenceId = subsequences[j].Id;

                        if (!db.Characteristic.Any(c => c.SequenceId == subsequenceId && c.CharacteristicTypeLinkId == secondCharacteristicTypeLinkId))
                        {
                            double value = calculator.Calculate(sequences[j], link);
                            var currentCharacteristic = new Characteristic
                            {
                                SequenceId = subsequenceId,
                                CharacteristicTypeLinkId = secondCharacteristicTypeLinkId,
                                Value = value,
                                ValueString = value.ToString()
                            };

                            db.Characteristic.Add(currentCharacteristic);
                            db.SaveChanges();
                        }
                    }

                    for (int d = 0; d < sequences.Count; d++)
                    {
                        long subsequenceId = subsequences[d].Id;
                        double characteristic = db.Characteristic.Single(c => c.SequenceId == subsequenceId && c.CharacteristicTypeLinkId == secondCharacteristicTypeLinkId).Value;

                        var geneCharacteristic = new SubsequenceCharacteristic(subsequences[d], characteristic, sequenceAttributeRepository.GetAttributes(subsequences[d].Id));
                        subsequencesCharacteristics.Add(geneCharacteristic);
                    }

                    subsequencesCharacteristics = subsequencesCharacteristics.OrderBy(g => g.Characteristic).ToList();

                    sequenceCharacterisitcs.Add(new SequenceCharacteristics(matterName, sequenceCharacteristic, subsequencesCharacteristics));
                }

                sequenceCharacterisitcs = sequenceCharacterisitcs.OrderBy(r => r.Characteristic).ToList();

                var fullCharacteristicName = characteristicTypeLinkRepository.GetCharacteristicName(firstCharacteristicTypeLinkId, firstNotationId);
                var subsequencesCharacteristicName = characteristicTypeLinkRepository.GetCharacteristicName(secondCharacteristicTypeLinkId, secondNotationId);

                var result = new Dictionary<string, object>
                                 {
                                     { "result", sequenceCharacterisitcs },
                                     { "maxSubsequencesIndex", maxSubsequencesIndex },
                                     { "subsequencesCharacteristicName", subsequencesCharacteristicName },
                                     { "fullCharacteristicName", fullCharacteristicName }
                                 };

                return new Dictionary<string, object>
                {
                    { "data", JsonConvert.SerializeObject(result) },
                    { "angularController", "SubsequencesDistributionResultController" }
                };
            });
        }
    }
}
