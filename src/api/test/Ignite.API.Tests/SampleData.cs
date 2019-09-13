using System;
using System.Collections.Generic;
using System.Text;
using Ignite.API.Common.UXO;

namespace Ignite.API.Tests
{
    public class SampleData
    {
        public static List<UXOMapItem> SampleMapItems()
        {
            var items = new List<UXOMapItem>();
            items.Add(new UXOMapItem()
            {
                Id = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                Symbol = "2525C"
            });
            return items;
        }

        public static UXO MinimalUXO()
        {
            var item = new UXO() {
                Id = "1"
            };

            return item;
        }

        public static byte[] SampleFile()
        {
            var file = Encoding.ASCII.GetBytes("Some text");
            return file;
        }
    }
}