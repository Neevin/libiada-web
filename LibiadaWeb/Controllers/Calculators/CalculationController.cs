﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using LibiadaWeb.Models;
using LibiadaWeb.Models.Repositories.Catalogs;
using LibiadaWeb.Models.Repositories.Chains;

namespace LibiadaWeb.Controllers.Calculators
{
    using LibiadaCore.Core;
    using LibiadaCore.Core.Characteristics;
    using LibiadaCore.Core.Characteristics.Calculators;

    public class CalculationController : Controller
    {
        private readonly LibiadaWebEntities db;
        private readonly MatterRepository matterRepository;
        private readonly CharacteristicTypeRepository characteristicRepository;
        private readonly NotationRepository notationRepository;
        private readonly ChainRepository chainRepository;

        public CalculationController()
        {
            db = new LibiadaWebEntities();
            matterRepository = new MatterRepository(db);
            characteristicRepository = new CharacteristicTypeRepository(db);
            notationRepository = new NotationRepository(db);
            chainRepository = new ChainRepository(db);
        }

        //
        // GET: /Calculation/

        public ActionResult Index()
        {
            IEnumerable<characteristic_type> characteristicsList =
                db.characteristic_type.Where(c => c.full_chain_applicable);

            var characteristicTypes = characteristicRepository.GetSelectListWithLinkable(characteristicsList);

            ViewBag.data = new Dictionary<string, object>
                {
                    {"matters", matterRepository.GetSelectListWithNature()},
                    {"characteristicTypes", characteristicTypes},
                    {"notations", notationRepository.GetSelectListWithNature()},
                    {"natures", new SelectList(db.nature, "id", "name")},
                    {"links", new SelectList(db.link, "id", "name")},
                    {"languages", new SelectList(db.language, "id", "name")},
                    {"natureLiterature", Aliases.NatureLiterature}
                };
            return View();
        }

        [HttpPost]
        public ActionResult Index(long[] matterIds, int[] characteristicIds, int?[] linkIds, int[] notationIds, int[] languageIds)
        {
            var characteristics = new List<List<Double>>();
            var chainNames = new List<string>();
            var characteristicNames = new List<string>();

            foreach (var matterId in matterIds)
            {
                chainNames.Add(db.matter.Single(m => m.id == matterId).name);

                characteristics.Add(new List<Double>());
                for (int i = 0; i < notationIds.Length; i++)
                {
                    int notationId = notationIds[i];
                    int languageId = languageIds[i];
                    long chainId;
                    if (db.matter.Single(m => m.id == matterId).nature_id == Aliases.NatureLiterature)
                    {
                        chainId = db.literature_chain.Single(l => l.matter_id == matterId &&
                                    l.notation_id == notationId
                                    && l.language_id == languageId).id;
                    }
                    else
                    {
                        chainId = db.chain.Single(c => c.matter_id == matterId && c.notation_id == notationId).id;
                    }

                    int characteristicId = characteristicIds[i];
                    int? linkId = linkIds[i];
                    if (db.characteristic.Any(c =>
                                              linkId == c.link.id && c.chain_id == chainId &&
                                              c.characteristic_type_id == characteristicId))
                    {
                        characteristic dbCharacteristic = db.characteristic.Single(c =>
                                                                       linkId == c.link.id && c.chain_id == chainId &&
                                                                       c.characteristic_type_id == characteristicId);
                        characteristics.Last().Add(dbCharacteristic.value.Value);
                    }
                    else
                    {
                        Chain tempChain = chainRepository.ToLibiadaChain(chainId);

                        String className =
                            db.characteristic_type.Single(ct => ct.id == characteristicId).class_name;
                        ICalculator calculator = CalculatorsFactory.Create(className);
                        var link = (Link) db.link.Single(l => l.id == linkId).id;
                        var characteristicValue = calculator.Calculate(tempChain, link);
                        int characteristicType = characteristicIds[i];
                        var dbCharacteristic = new characteristic
                            {
                                chain_id = chainId,
                                characteristic_type_id = characteristicIds[i],
                                link_id = db.characteristic_type.Single( c=> c.id == characteristicType).linkable ? linkId : null,
                                value = characteristicValue,
                                value_string = characteristicValue.ToString(),
                                created = DateTime.Now
                            };
                        db.characteristic.AddObject(dbCharacteristic);
                        db.SaveChanges();
                        characteristics.Last().Add(characteristicValue);
                    }
                }
            }

            for (int k = 0; k < characteristicIds.Length; k++)
            {
                int characteristicId = characteristicIds[k];
                int? linkId = linkIds[k];
                int notationId = notationIds[k];
                characteristicNames.Add(db.characteristic_type.Single(c => c.id == characteristicId).name + " " +
                                        db.link.Single(l => l.id == linkId).name + " " +
                                        db.notation.Single(n => n.id == notationId).name);
            }

            TempData["characteristics"] = characteristics;
            TempData["chainNames"] = chainNames;
            TempData["characteristicNames"] = characteristicNames;
            TempData["characteristicIds"] = characteristicIds;
            TempData["chainIds"] = new List<long>(matterIds);
            return RedirectToAction("Result");
        }

        public ActionResult Result()
        {
            try
            {
                var characteristics = TempData["characteristics"];
                var characteristicNames = TempData["characteristicNames"] as List<String>;
                ViewBag.chainIds = TempData["chainIds"] as List<long>;
                var characteristicIds = TempData["characteristicIds"] as int[];
                var characteristicsList = new List<SelectListItem>();
                for (int i = 0; i < characteristicNames.Count; i++)
                {
                    characteristicsList.Add(new SelectListItem
                    {
                        Value = i.ToString(),
                        Text = characteristicNames[i],
                        Selected = false
                    });
                }
                ViewBag.characteristicIds = new List<int>(characteristicIds);
                ViewBag.characteristicsList = characteristicsList;
                ViewBag.characteristics = characteristics;
                ViewBag.chainNames = TempData["chainNames"] as List<String>;
                ViewBag.characteristicNames = characteristicNames;
                TempData.Keep();
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
            }

            return View();
        }
    }
}
