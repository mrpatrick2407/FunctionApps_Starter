using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Model
{
    public class ImageModel
    {
        public enum ImageSize { ExtraSmall, Small, Medium }
        public static Dictionary<ImageSize, (int, int)>
        imageDimensionsTable = new Dictionary<ImageSize, (int, int)>()
         {
                    { ImageSize.Small,      (100, 100) },
                    { ImageSize.Medium,     (200, 200) },

         };
    }
}
