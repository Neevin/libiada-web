﻿namespace LibiadaWeb.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.Mvc.Html;

    using LibiadaWeb.Models;
    using LibiadaWeb.Models.Account;
    using LibiadaWeb.Models.Calculators;
    using LibiadaWeb.Models.CalculatorsData;
    using LibiadaWeb.Models.Repositories.Catalogs;
    using LibiadaWeb.Models.Repositories.Sequences;

    using Link = LibiadaCore.Core.Link;

    /// <summary>
    /// Class filling data for ViewBag.
    /// </summary>
    public class ViewDataHelper
    {
        /// <summary>
        /// The db.
        /// </summary>
        private readonly LibiadaWebEntities db;

        /// <summary>
        /// The matter repository.
        /// </summary>
        private readonly MatterRepository matterRepository;

        /// <summary>
        /// The notation repository.
        /// </summary>
        private readonly NotationRepository notationRepository;

        /// <summary>
        /// The feature repository.
        /// </summary>
        private readonly FeatureRepository featureRepository;

        /// <summary>
        /// The characteristic type link repository.
        /// </summary>
        private readonly CharacteristicTypeLinkRepository characteristicTypeLinkRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataHelper"/> class.
        /// </summary>
        /// <param name="db">
        /// The db.
        /// </param>
        public ViewDataHelper(LibiadaWebEntities db)
        {
            this.db = db;
            matterRepository = new MatterRepository(db);
            notationRepository = new NotationRepository(db);
            featureRepository = new FeatureRepository(db);
            characteristicTypeLinkRepository = new CharacteristicTypeLinkRepository(db);
        }

        /// <summary>
        /// The fill calculation data.
        /// </summary>
        /// <param name="minimumSelectedMatters">
        /// The minimum Selected Matters.
        /// </param>
        /// <param name="maximumSelectedMatters">
        /// The maximum Selected Matters.
        /// </param>
        /// <param name="submitName">
        /// The submit button name.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, Object}"/>.
        /// </returns>
        public Dictionary<string, object> FillViewData(int minimumSelectedMatters, int maximumSelectedMatters, string submitName)
        {
            var translators = new SelectList(db.Translator, "id", "name").ToList();
            translators.Insert(0, new SelectListItem { Value = null, Text = "Not applied" });

            var data = FillMattersData(minimumSelectedMatters, maximumSelectedMatters, m => true, submitName);

            IEnumerable<SelectListItem> natures;
            IEnumerable<object> notations;
            IEnumerable<SelectListItemWithNature> sequenceTypes;
            IEnumerable<SelectListItemWithNature> groups;

            if (UserHelper.IsAdmin())
            {
                natures = EnumHelper.GetSelectList(typeof(Nature));
                notations = notationRepository.GetSelectListWithNature();
                sequenceTypes = EnumExtensions.ToArray<SequenceType>()
                    .Select(st => new SelectListItemWithNature { Text = st.GetDisplayValue(), Value = st.GetDisplayValue(), Nature = (byte)st.GetAttribute<SequenceType, NatureAttribute>().Value });
                groups = EnumExtensions.ToArray<Group>()
                    .Select(g => new SelectListItemWithNature { Text = g.GetDisplayValue(), Value = g.GetDisplayValue(), Nature = (byte)g.GetAttribute<Group, NatureAttribute>().Value });
            }
            else
            {
                natures = new List<Nature> { Nature.Genetic }.ToSelectList();
                notations = notationRepository.GetSelectListWithNature(new List<int> { Aliases.Notation.Nucleotide });
                sequenceTypes = EnumExtensions.ToArray<SequenceType>()
                    .Where(st => st.GetAttribute<SequenceType, NatureAttribute>().Value == Nature.Genetic)
                    .Select(st => new SelectListItemWithNature { Text = st.GetDisplayValue(), Value = st.GetDisplayValue(), Nature = (byte)st.GetAttribute<SequenceType, NatureAttribute>().Value });
                groups = EnumExtensions.ToArray<Group>()
                    .Where(g => g.GetAttribute<Group, NatureAttribute>().Value == Nature.Genetic)
                    .Select(g => new SelectListItemWithNature { Text = g.GetDisplayValue(), Value = g.GetDisplayValue(), Nature = (byte)g.GetAttribute<Group, NatureAttribute>().Value });
            }

            data.Add("natures",  natures);
            data.Add("notations", notations);
            data.Add("languages", new SelectList(db.Language, "id", "name"));
            data.Add("translators", translators);
            data.Add("sequenceTypes", sequenceTypes);
            data.Add("groups", groups);

            return data;
        }

        /// <summary>
        /// The fill calculation data.
        /// </summary>
        /// <param name="filter">
        /// The filter.
        /// </param>
        /// <param name="minimumSelectedMatters">
        /// The minimum Selected Matters.
        /// </param>
        /// <param name="maximumSelectedMatters">
        /// The maximum Selected Matters.
        /// </param>
        /// <param name="submitName">
        /// The submit button name.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, Object}"/>.
        /// </returns>
        public Dictionary<string, object> FillViewData(Func<CharacteristicType, bool> filter, int minimumSelectedMatters, int maximumSelectedMatters, string submitName)
        {
            var data = FillViewData(minimumSelectedMatters, maximumSelectedMatters, submitName);
            data.Add("characteristicTypes", GetCharacteristicTypes(filter));

            return data;
        }

        /// <summary>
        /// The get subsequences calculation data.
        /// </summary>
        /// <param name="minimumSelectedMatters">
        /// The minimum Selected Matters.
        /// </param>
        /// <param name="maximumSelectedMatters">
        /// The maximum Selected Matters.
        /// </param>
        /// <param name="submitName">
        /// The submit button name.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, Object}"/>.
        /// </returns>
        public Dictionary<string, object> GetSubsequencesViewData(int minimumSelectedMatters, int maximumSelectedMatters, string submitName)
        {
            var featureIds = featureRepository.Features.Where(f => f.Nature == Nature.Genetic && !f.Complete).Select(f => f.Id);

            var sequenceIds = db.Subsequence.Select(s => s.SequenceId).Distinct();
            var matterIds = db.DnaSequence.Where(c => sequenceIds.Contains(c.Id)).Select(c => c.MatterId).ToList();

            var data = FillMattersData(minimumSelectedMatters, maximumSelectedMatters, m => matterIds.Contains(m.Id), submitName);

            var geneticNotations = db.Notation.Where(n => n.Nature == Nature.Genetic).Select(n => n.Id).ToList();
            var characteristicTypes = GetCharacteristicTypes(c => c.FullSequenceApplicable);
            var sequenceTypes = EnumExtensions.ToArray<SequenceType>()
                    .Where(st => st.GetAttribute<SequenceType, NatureAttribute>().Value == Nature.Genetic)
                    .Select(st => new SelectListItemWithNature { Text = st.GetDisplayValue(), Value = st.GetDisplayValue(), Nature = (byte)st.GetAttribute<SequenceType, NatureAttribute>().Value });
            var groups = EnumExtensions.ToArray<Group>()
                .Where(g => g.GetAttribute<Group, NatureAttribute>().Value == Nature.Genetic)
                .Select(g => new SelectListItemWithNature { Text = g.GetDisplayValue(), Value = g.GetDisplayValue(), Nature = (byte)g.GetAttribute<Group, NatureAttribute>().Value });

            data.Add("characteristicTypes", characteristicTypes);
            data.Add("notations", notationRepository.GetSelectListWithNature(geneticNotations));
            data.Add("nature", (byte)Nature.Genetic);
            data.Add("features", featureRepository.GetSelectListWithNature(featureIds, featureIds));
            data.Add("sequenceTypes", sequenceTypes);
            data.Add("groups", groups);

            return data;
        }

        /// <summary>
        /// The get characteristic types.
        /// </summary>
        /// <param name="filter">
        /// The filter.
        /// </param>
        /// <returns>
        /// The <see cref="List{CharacteristicData}"/>.
        /// </returns>
        public List<CharacteristicData> GetCharacteristicTypes(Func<CharacteristicType, bool> filter)
        {
            var characteristicTypes = db.CharacteristicType.Include(c => c.CharacteristicTypeLink).Where(filter).OrderBy(c => c.Name)
                .Select(c => new CharacteristicData(c.Id, c.Name, c.CharacteristicTypeLink.OrderBy(ctl => ctl.LinkId).Select(ctl => new CharacteristicLinkData(ctl.Id)).ToList())).ToList();

            var links = UserHelper.IsAdmin() ? EnumExtensions.ToArray<Link>() : new[] { Link.NotApplied, Link.Start, Link.Cycle };

            var characteristicTypeLinks = characteristicTypeLinkRepository.CharacteristicTypeLinks;

            var linksData = links.Select(l => new
                                                {
                                                    Value = (int)l,
                                                    Text = l.GetDisplayValue(),
                                                    CharacteristicTypeLink = characteristicTypeLinks.Where(ctl => ctl.LinkId == (int)l).Select(ctl => ctl.Id)
                                                });

            foreach (var characteristicType in characteristicTypes)
            {
                for (int i = 0; i < characteristicType.CharacteristicLinks.Count; i++)
                {
                    var characteristicLink = characteristicType.CharacteristicLinks[i];
                    foreach (var link in linksData)
                    {
                        if (link.CharacteristicTypeLink.Contains(characteristicLink.CharacteristicTypeLinkId))
                        {
                            characteristicLink.Value = link.Value.ToString();
                            characteristicLink.Text = link.Text;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(characteristicLink.Value))
                    {
                        characteristicType.CharacteristicLinks.Remove(characteristicLink);
                        i--;
                    }
                }
            }

            return characteristicTypes;
        }

        /// <summary>
        /// The fill matters data.
        /// </summary>
        /// <param name="minimumSelectedMatters">
        /// The minimum selected matters.
        /// </param>
        /// <param name="maximumSelectedMatters">
        /// The maximum selected matters.
        /// </param>
        /// <param name="filter">
        /// Filter for matters.
        /// </param>
        /// <param name="submitName">
        /// The submit button name.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, Object}"/>.
        /// </returns>
        public Dictionary<string, object> FillMattersData(int minimumSelectedMatters, int maximumSelectedMatters, Func<Matter, bool> filter, string submitName)
        {
            var radiobuttonsForMatters = maximumSelectedMatters == 1 && minimumSelectedMatters == 1;

            return new Dictionary<string, object>
                {
                    { "minimumSelectedMatters", minimumSelectedMatters },
                    { "maximumSelectedMatters", maximumSelectedMatters },
                    { "matters", matterRepository.GetMatterSelectList(filter) },
                    { "radiobuttonsForMatters", radiobuttonsForMatters },
                    { "submitName", submitName }
                };
        }
    }
}
