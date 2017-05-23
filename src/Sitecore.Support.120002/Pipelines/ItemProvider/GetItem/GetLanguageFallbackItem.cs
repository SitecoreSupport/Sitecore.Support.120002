using Sitecore;
using Sitecore.Collections;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Pipelines.ItemProvider.GetItem;
using System;
using System.Collections.Generic;

namespace Sitecore.Support.Pipelines.ItemProvider.GetItem
{
    public class GetLanguageFallbackItem : GetItemProcessor
    {
        public override void Process(GetItemArgs args)
        {
            bool? currentValue = Switcher<bool?, LanguageFallbackItemSwitcher>.CurrentValue;
            if ((currentValue == false || !args.AllowLanguageFallback) && currentValue != true)
            {
                return;
            }
            if (args.Result == null && args.Handled)
            {
                return;
            }
            Item item = args.Result ?? ((args.ItemId != (ShortID)null) ? args.FallbackProvider.GetItem(args.ItemId, args.Language, args.Version, args.Database, args.SecurityCheck) : args.FallbackProvider.GetItem(args.ItemPath, args.Language, args.Version, args.Database, args.SecurityCheck));
            args.Result = item;
            if (!this.IsItemFallbackEnabled(item))
            {
                return;
            }
            System.Collections.Generic.List<Language> list = new System.Collections.Generic.List<Language>(4);
            Item item2 = item;
            Language language = args.Language;
            while (item2 != null && (!item2.Name.StartsWith("__") || StandardValuesManager.IsStandardValuesHolder(item2)) && item2.RuntimeSettings.TemporaryVersion)
            {
                list.Add(language);
                language = LanguageFallbackManager.GetFallbackLanguage(language, args.Database);
                if (language == null || list.Contains(language))
                {
                    return;
                }
                item2 = args.FallbackProvider.GetItem(item2.ID, language, Sitecore.Data.Version.Latest, args.Database, args.SecurityCheck);
            }
            if (item2 == null || language == args.Language)
            {
                return;
            }
            ItemData data = new ItemData(item2.InnerData.Definition, item.Language, item.Version, item2.InnerData.Fields);
            Item result = new Item(item.ID, data, item.Database)
            {
                OriginalLanguage = item2.Language
            };

            if (item.IsItemClone)
            {

                FieldCollection fcoll = item2.Source.Fields;
                FieldList flist2 = new FieldList();

                foreach (Field field in fcoll)
                {
                    if (item2.Fields[field.Name] != null && !(field.Name.StartsWith("__")))
                    {
                        flist2.Add(item2.Fields[field.Name].ID, item2.Fields[field.Name].Value);

                    }
                    else
                    {
                        flist2.Add(field.ID, field.Value);
                    }
                }

                ItemData data1 = new ItemData(item2.InnerData.Definition, item.Language, item.Version, flist2);


                result = new Item(item.ID, data1, item.Database)
                {
                    OriginalLanguage = item2.Language
                };


            }

            args.Result = result;

        }

        private bool IsItemFallbackEnabled(Item item)
        {
            if (item == null)
            {
                return false;
            }
            if (StandardValuesManager.IsStandardValuesHolder(item))
            {
                return item.Fields[FieldIDs.EnableItemFallback].GetValue(false) == "1";
            }
            bool result;
            using (new LanguageFallbackItemSwitcher(new bool?(false)))
            {
                result = (item.Fields[FieldIDs.EnableItemFallback].GetValue(true, true, false) == "1");
            }
            return result;
        }
    }
}