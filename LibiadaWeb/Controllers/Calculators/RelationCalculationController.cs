﻿namespace LibiadaWeb.Controllers.Calculators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using Helpers;

    using LibiadaCore.Core;
    using LibiadaCore.Core.Characteristics;
    using LibiadaCore.Core.Characteristics.Calculators;

    using Models;
    using Models.Repositories;
    using Models.Repositories.Catalogs;
    using Models.Repositories.Chains;

    /// <summary>
    /// The relation calculation controller.
    /// </summary>
    public class RelationCalculationController : AbstractResultController
    {
        /// <summary>
        /// The db.
        /// </summary>
        private readonly LibiadaWebEntities db;

        /// <summary>
        /// The characteristic repository.
        /// </summary>
        private readonly CharacteristicTypeRepository characteristicRepository;

        /// <summary>
        /// The link repository.
        /// </summary>
        private readonly LinkRepository linkRepository;

        /// <summary>
        /// The chain repository.
        /// </summary>
        private readonly CommonSequenceRepository commonSequenceRepository;

        /// <summary>
        /// The binary characteristic repository.
        /// </summary>
        private readonly BinaryCharacteristicRepository binaryCharacteristicRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationCalculationController"/> class.
        /// </summary>
        public RelationCalculationController() : base("RelationCalculation", "Relation calculation")
        {
            db = new LibiadaWebEntities();
            characteristicRepository = new CharacteristicTypeRepository(db);
            linkRepository = new LinkRepository(db);
            commonSequenceRepository = new CommonSequenceRepository(db);
            binaryCharacteristicRepository = new BinaryCharacteristicRepository(db);
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        public ActionResult Index()
        {
            ViewBag.dbName = DbHelper.GetDbName(db);
            var sequences = db.CommonSequence.Include("matter");
            ViewBag.chainCheckBoxes = commonSequenceRepository.GetSelectListItems(sequences, null);
            ViewBag.chains = sequences;
            var languages = new List<string>();
            var fastaHeaders = new List<string>();
            foreach (var sequence in sequences)
            {
                languages.Add(sequence.Matter.Nature.Id == Aliases.Nature.Literature
                                         ? db.LiteratureSequence.Single(l => l.Id == sequence.Id).Language.Name
                                         : null);
                fastaHeaders.Add(sequence.Matter.Nature.Id == Aliases.Nature.Genetic
                                         ? db.DnaSequence.Single(l => l.Id == sequence.Id).FastaHeader
                                         : null);
            }

            ViewBag.languages = languages;
            ViewBag.fastaHeaders = fastaHeaders;

            ViewBag.chainsList = commonSequenceRepository.GetSelectListItems(null);
            var characteristics = db.CharacteristicType.Where(c => c.BinarySequenceApplicable);
            ViewBag.characteristicsList = characteristicRepository.GetSelectListItems(characteristics, null);
            ViewBag.linksList = linkRepository.GetSelectListItems(null);
            return View();
        }

        /// <summary>
        /// The index.
        /// </summary>
        /// <param name="chainId">
        /// The chain id.
        /// </param>
        /// <param name="characteristicId">
        /// The characteristic id.
        /// </param>
        /// <param name="linkId">
        /// The link id.
        /// </param>
        /// <param name="filterSize">
        /// The filter size.
        /// </param>
        /// <param name="filter">
        /// The filter.
        /// </param>
        /// <param name="frequency">
        /// The frequency.
        /// </param>
        /// <param name="frequencyCount">
        /// The frequency count.
        /// </param>
        /// <param name="oneWord">
        /// The one word.
        /// </param>
        /// <param name="wordId">
        /// The word id.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        public ActionResult Index(
            long chainId, 
            int characteristicId, 
            int linkId, 
            int filterSize, 
            bool filter, 
            bool frequency, 
            int frequencyCount, 
            bool oneWord, 
            long wordId = 0)
        {
            return Action(() =>
            {
                var characteristics = new List<BinaryCharacteristic>();
                var elements = new List<Element>();
                List<BinaryCharacteristic> filteredResult = null;
                List<BinaryCharacteristic> filteredResult1 = null;
                List<BinaryCharacteristic> filteredResult2 = null;
                var firstElements = new List<Element>();
                var secondElements = new List<Element>();
                string word = null;

                CommonSequence dbSequence = db.CommonSequence.Single(c => c.Id == chainId);

                Chain currentChain = commonSequenceRepository.ToLibiadaChain(dbSequence.Id);
                string className = db.CharacteristicType.Single(c => c.Id == characteristicId).ClassName;

                IBinaryCalculator calculator = CalculatorsFactory.CreateBinaryCalculator(className);
                var link = (Link)linkId;

                if (oneWord)
                {
                    word = OneWordCharacteristic(characteristicId, linkId, wordId, dbSequence, currentChain, calculator, link);

                    filteredResult1 = db.BinaryCharacteristic.Where(b => b.SequenceId == dbSequence.Id &&
                                                                          b.CharacteristicTypeId == characteristicId &&
                                                                          b.LinkId == linkId &&
                                                                          b.FirstElementId == wordId)
                        .OrderBy(b => b.SecondElementId)
                        .ToList();

                    filteredResult2 = db.BinaryCharacteristic.Where(b => b.SequenceId == dbSequence.Id &&
                                                                          b.CharacteristicTypeId == characteristicId &&
                                                                          b.LinkId == linkId &&
                                                                          b.SecondElementId == wordId)
                        .OrderBy(b => b.FirstElementId)
                        .ToList();

                    for (int l = 0; l < currentChain.Alphabet.Cardinality; l++)
                    {
                        long elementId = filteredResult1[l].SecondElementId;
                        elements.Add(db.Element.Single(e => e.Id == elementId));
                    }
                }
                else
                {
                    if (frequency)
                    {
                        FrequencyCharacteristic(characteristicId, linkId, frequencyCount, currentChain, dbSequence, calculator, link);
                    }
                    else
                    {
                        NotFrequencyCharacteristic(characteristicId, linkId, dbSequence, currentChain, calculator, link);
                    }

                    if (filter)
                    {
                        filteredResult = db.BinaryCharacteristic.Where(b => b.SequenceId == dbSequence.Id &&
                                                                             b.CharacteristicTypeId == characteristicId &&
                                                                             b.LinkId == linkId)
                            .OrderByDescending(b => b.Value)
                            .Take(filterSize).ToList();

                        for (int l = 0; l < filterSize; l++)
                        {
                            long firstElementId = filteredResult[l].FirstElementId;
                            firstElements.Add(db.Element.Single(e => e.Id == firstElementId));
                        }

                        for (int m = 0; m < filterSize; m++)
                        {
                            long secondElementId = filteredResult[m].SecondElementId;
                            secondElements.Add(db.Element.Single(e => e.Id == secondElementId));
                        }
                    }
                    else
                    {
                        characteristics = db.BinaryCharacteristic.Where(b => b.SequenceId == dbSequence.Id &&
                                                                              b.CharacteristicTypeId == characteristicId &&
                                                                              b.LinkId == linkId)
                            .OrderBy(b => b.SecondElementId)
                            .ThenBy(b => b.FirstElementId)
                            .ToList();
                        for (int m = 0; m < Math.Sqrt(characteristics.Count()); m++)
                        {
                            long firstElementId = characteristics[m].FirstElementId;
                            elements.Add(db.Element.Single(e => e.Id == firstElementId));
                        }
                    }
                }

                return new Dictionary<string, object>
                {
                    { "characteristics", characteristics },
                    { "isFilter", filter },
                    { "filteredResult", filteredResult },
                    { "firstElements", firstElements },
                    { "secondElements", secondElements },
                    { "filterSize", filterSize },
                    { "elements", elements },
                    { "characteristicName", db.CharacteristicType.Single(charact => charact.Id == characteristicId).Name },
                    { "chainName", db.CommonSequence.Single(m => m.Id == chainId).Matter.Name },
                    { "notationName", db.CommonSequence.Single(c => c.Id == chainId).Notation.Name },
                    { "filteredResult1", filteredResult1 },
                    { "filteredResult2", filteredResult2 },
                    { "oneWord", oneWord },
                    { "word", word }
                };
            });
        }

        /// <summary>
        /// The one word characteristic.
        /// </summary>
        /// <param name="characteristicId">
        /// The characteristic id.
        /// </param>
        /// <param name="linkId">
        /// The link id.
        /// </param>
        /// <param name="wordId">
        /// The word id.
        /// </param>
        /// <param name="dbSequence">
        /// The db chain.
        /// </param>
        /// <param name="currentChain">
        /// The current chain.
        /// </param>
        /// <param name="calculator">
        /// The calculator.
        /// </param>
        /// <param name="link">
        /// The link.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string OneWordCharacteristic(
            int characteristicId,
            int linkId,
            long wordId,
            CommonSequence dbSequence,
            Chain currentChain,
            IBinaryCalculator calculator,
            Link link)
        {
            int calculatedCount = db.BinaryCharacteristic.Count(b => b.SequenceId == dbSequence.Id &&
                                                                      b.CharacteristicTypeId == characteristicId &&
                                                                      b.LinkId == linkId && b.FirstElementId == wordId);
            List<long> sequenceElements = commonSequenceRepository.GetElementIds(dbSequence.Id);
            if (calculatedCount < currentChain.Alphabet.Cardinality)
            {
                // TODO: проверить что + - 1 нигде к индексам не надо добавлять
                int firstElementNumber = sequenceElements.IndexOf(wordId);
                for (int i = 0; i < currentChain.Alphabet.Cardinality; i++)
                {
                    long secondElementId = sequenceElements[i];
                    if (!db.BinaryCharacteristic.Any(b =>
                                                      b.SequenceId == dbSequence.Id &&
                                                      b.CharacteristicTypeId == characteristicId &&
                                                      b.FirstElementId == wordId &&
                                                      b.SecondElementId == secondElementId &&
                                                      b.LinkId == linkId))
                    {
                        double result = calculator.Calculate(currentChain.GetRelationIntervalsManager(firstElementNumber + 1, i + 1), link);

                        binaryCharacteristicRepository.CreateBinaryCharacteristic(
                            dbSequence.Id,
                            characteristicId,
                            linkId,
                            wordId,
                            secondElementId,
                            result);
                    }
                }
            }

            calculatedCount = db.BinaryCharacteristic.Count(b => b.SequenceId == dbSequence.Id &&
                                                                  b.CharacteristicTypeId == characteristicId &&
                                                                  b.LinkId == linkId && b.SecondElementId == wordId);

            if (calculatedCount < currentChain.Alphabet.Cardinality)
            {
                int secondElementNumber = sequenceElements.IndexOf(wordId);
                for (int i = 0; i < currentChain.Alphabet.Cardinality; i++)
                {
                    long firstElementId = sequenceElements[i];
                    if (!db.BinaryCharacteristic.Any(b =>
                                                      b.SequenceId == dbSequence.Id &&
                                                      b.CharacteristicTypeId == characteristicId &&
                                                      b.FirstElementId == firstElementId &&
                                                      b.SecondElementId == wordId &&
                                                      b.LinkId == linkId))
                    {
                        double result = calculator.Calculate(currentChain.GetRelationIntervalsManager(secondElementNumber, i + 1), link);
                        binaryCharacteristicRepository.CreateBinaryCharacteristic(
                            dbSequence.Id,
                            characteristicId,
                            linkId,
                            firstElementId,
                            wordId,
                            result);
                    }
                }
            }

            return db.Element.Single(e => e.Id == wordId).Name;
        }

        /// <summary>
        /// The not frequency characteristic.
        /// </summary>
        /// <param name="characteristicId">
        /// The characteristic id.
        /// </param>
        /// <param name="linkId">
        /// The link id.
        /// </param>
        /// <param name="dbSequence">
        /// The db chain.
        /// </param>
        /// <param name="currentChain">
        /// The current chain.
        /// </param>
        /// <param name="calculator">
        /// The calculator.
        /// </param>
        /// <param name="link">
        /// The link.
        /// </param>
        private void NotFrequencyCharacteristic(
            int characteristicId,
            int linkId,
            CommonSequence dbSequence,
            Chain currentChain,
            IBinaryCalculator calculator,
            Link link)
        {
            int calculatedCount = db.BinaryCharacteristic.Count(b => b.SequenceId == dbSequence.Id &&
                                                                 b.CharacteristicTypeId == characteristicId &&
                                                                 b.LinkId == linkId);
            if (calculatedCount < currentChain.Alphabet.Cardinality * currentChain.Alphabet.Cardinality)
            {
                List<long> sequenceElements = commonSequenceRepository.GetElementIds(dbSequence.Id);
                for (int i = 0; i < currentChain.Alphabet.Cardinality; i++)
                {
                    for (int j = 0; j < currentChain.Alphabet.Cardinality; j++)
                    {
                        long firstElementId = sequenceElements[i];
                        long secondElementId = sequenceElements[i];
                        if (!db.BinaryCharacteristic.Any(b =>
                                                          b.SequenceId == dbSequence.Id &&
                                                          b.CharacteristicTypeId == characteristicId &&
                                                          b.FirstElementId == firstElementId &&
                                                          b.SecondElementId == secondElementId &&
                                                          b.LinkId == linkId))
                        {
                            double result = calculator.Calculate(currentChain.GetRelationIntervalsManager(i + 1, j + 1), link);

                            binaryCharacteristicRepository.CreateBinaryCharacteristic(
                                dbSequence.Id,
                                characteristicId,
                                linkId,
                                firstElementId,
                                secondElementId,
                                result);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The frequency characteristic.
        /// </summary>
        /// <param name="characteristicId">
        /// The characteristic id.
        /// </param>
        /// <param name="linkId">
        /// The link id.
        /// </param>
        /// <param name="frequencyCount">
        /// The frequency count.
        /// </param>
        /// <param name="currentChain">
        /// The current chain.
        /// </param>
        /// <param name="dbSequence">
        /// The db chain.
        /// </param>
        /// <param name="calculator">
        /// The calculator.
        /// </param>
        /// <param name="link">
        /// The link.
        /// </param>
        private void FrequencyCharacteristic(
            int characteristicId,
            int linkId,
            int frequencyCount,
            Chain currentChain,
            CommonSequence dbSequence,
            IBinaryCalculator calculator,
            Link link)
        {
            List<long> sequenceElements = commonSequenceRepository.GetElementIds(dbSequence.Id);

            // считаем частоты слов
            var frequences = new List<KeyValuePair<IBaseObject, double>>();
            for (int f = 0; f < currentChain.Alphabet.Cardinality; f++)
            {
                var calc = new Probability();
                frequences.Add(new KeyValuePair<IBaseObject, double>(
                        currentChain.Alphabet[f],
                        calc.Calculate(currentChain.CongenericChain(f), Link.Both)));
            }

            // сортируем алфавит по частоте
            SortKeyValuePairList(frequences);

            // для заданного числа слов с наибольшей частотой считаем зависимости
            for (int i = 0; i < frequencyCount; i++)
            {
                for (int j = 0; j < frequencyCount; j++)
                {
                    int firstElementNumber = currentChain.Alphabet.IndexOf(frequences[i].Key) + 1;
                    int secondElementNumber = currentChain.Alphabet.IndexOf(frequences[j].Key) + 1;
                    long firstElementId = sequenceElements[firstElementNumber];
                    long secondElementId = sequenceElements[secondElementNumber];

                    // проверяем не посчитана ли уже эта характеристика
                    if (!db.BinaryCharacteristic.Any(b =>
                                                      b.SequenceId == dbSequence.Id &&
                                                      b.CharacteristicTypeId == characteristicId &&
                                                      b.FirstElementId == firstElementId &&
                                                      b.SecondElementId == secondElementId &&
                                                      b.LinkId == linkId))
                    {
                        double result = calculator.Calculate(currentChain.GetRelationIntervalsManager(i + 1, j + 1), link);

                        binaryCharacteristicRepository.CreateBinaryCharacteristic(
                            dbSequence.Id,
                            characteristicId,
                            linkId,
                            firstElementId,
                            secondElementId,
                            result);
                    }
                }
            }
        }

        /// <summary>
        /// The sort key value pair list.
        /// </summary>
        /// <param name="arrayForSort">
        /// The array for sort.
        /// </param>
        private void SortKeyValuePairList(List<KeyValuePair<IBaseObject, double>> arrayForSort)
        {
            arrayForSort.Sort(
                (firstPair, nextPair) => nextPair.Value.CompareTo(firstPair.Value));
        }
    }
}
