using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework.Pipelines;
using System;

namespace Sitecore.Support.Shell.Framework.Pipelines.CloneItems
{
    public class RemoveExtraVersions : CopyItems
    {
        public override void Execute(CopyItemsArgs args)
        {
            foreach (Item item in args.Copies)
            {
                using (new SecurityDisabler())
                {
                    Language[] languages = item.Languages;
                    foreach (Language language in languages)
                    {
                        if (item.Database.GetItem(item.Source.ID, language, Sitecore.Data.Version.Latest).IsFallback)
                        {
                            item.Database.GetItem(item.ID, language, Sitecore.Data.Version.Latest).Versions.RemoveVersion();
                        }
                    }
                }
            }
        }
    }
}