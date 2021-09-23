using System;
using System.IO;
using System.IO.Packaging;
using System.Net;

namespace ContainerDesktop.Common
{
    public static class ResourceUtilities
    {
        public static Stream GetPackContent(Uri packUri)
        {
            IWebRequestCreate factory = new PackWebRequestFactory();
            var request = factory.Create(packUri);
            var response = request.GetResponse();
            return response.GetResponseStream();
        }
    }
}
