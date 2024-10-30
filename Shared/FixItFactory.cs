namespace Shared
{
    public static class FixItFactory
    {
        public static FixIt ImportMissing(string import, Metadata meta)
        {
            return new FixIt {
                Message = $"Add missing import {import}",
                Code = $"import {import}",
                Meta = meta
            };
        }
    }
}
