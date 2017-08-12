using Sitecore.Collections;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Pipelines.ItemProvider.GetItem;
using System.Collections.Generic;
using Sitecore.StringExtensions;

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
            List<Language> list = new List<Language>(4);
            Item fallbackItem = item;
            Language language = args.Language;
            while (fallbackItem != null && (!fallbackItem.Name.StartsWith("__") || StandardValuesManager.IsStandardValuesHolder(fallbackItem)) && fallbackItem.RuntimeSettings.TemporaryVersion)
            {
                list.Add(language);
                language = LanguageFallbackManager.GetFallbackLanguage(language, args.Database);
                if (language == null || list.Contains(language))
                {
                    return;
                }
                fallbackItem = args.FallbackProvider.GetItem(fallbackItem.ID, language, Sitecore.Data.Version.Latest, args.Database, args.SecurityCheck);
            }
            if (fallbackItem == null || language == args.Language)
            {
                return;
            }
            FieldList fieldList = fallbackItem.InnerData.Fields; //from fallback item version
            

            bool isItemClone;

            using (new LanguageFallbackItemSwitcher(false))
            {
                isItemClone = item.IsItemClone;
            }

            if (isItemClone)
            {

                FieldCollection fields = fallbackItem.Source.Fields; //from fallback item version source
                fieldList = new FieldList();

                foreach (Field field in fields)
                {
                    if (!(field.Name.IsNullOrEmpty() || field.Name.StartsWith("__")) && fallbackItem.Fields[field.Name] != null)
                    {
                        fieldList.Add(fallbackItem.Fields[field.Name].ID, fallbackItem.Fields[field.Name].Value);
                    }
                    else
                    {
                        fieldList.Add(field.ID, field.Value);
                    }
                }
            }

            ItemData data = new ItemData(fallbackItem.InnerData.Definition, item.Language, item.Version, fieldList);
            Item result = new Item(item.ID, data, item.Database)
            {
                OriginalLanguage = fallbackItem.Language
            };
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