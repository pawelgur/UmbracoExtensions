using Newtonsoft.Json.Linq;
using Umbraco;
using Umbraco.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;
using umbraco;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Core.Logging;
using umbraco.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

namespace PG.UmbracoExtensions.Helpers
{
    public static class ImageHelpers
    {
        static HttpServerUtility Server = HttpContext.Current.Server;
        static IMediaService MediaService = ApplicationContext.Current.Services.MediaService;



        public static string GetThumbnailUrl(this IPublishedContent node, string cropName = "")
        {
            return GetImageFieldUrl(node, "thumbnail", cropName);
        }


        public static string GetImageUrl(this IPublishedContent node, string cropName = "")
        {
            return GetImageFieldUrl(node, "image", cropName);
        }


        /// <summary>
        /// Gets specified dimensions crop url
        /// </summary>
        public static string GetImageUrl(this IPublishedContent node, int cropWidth, int cropHeight, string fieldAlias = "image")
        {
            var result = "";

            if (node.HasValue(fieldAlias))
            {
                var mediaItem = GetMediaItem(node, fieldAlias);

                result = mediaItem.GetCropUrl(width: cropWidth, height: cropHeight);
            }

            return result;
        }


        /// <summary>
        /// Gets images url list.
        /// 
        /// Gets crops if cropname specified
        /// </summary>
        public static IEnumerable<string> GetImagesUrl(IPublishedContent post, string cropName = "", string fieldAlias = "images")
        {
            List<string> result = new List<string>();

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            try
            {
                var pictureIDsVal = post.GetPropertyValue<string>(fieldAlias);
                if (!String.IsNullOrEmpty(pictureIDsVal))
                {
                    var pictureIDs = pictureIDsVal.Split(new [] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pictureIDstr in pictureIDs)
                    {
                        if (!String.IsNullOrEmpty(pictureIDstr))
                        {
                            int id = Int32.Parse(pictureIDstr);
                            var url = GetImageUrl(id, cropName);
                            result.Add(url);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                if (UmbracoContext.Current.IsDebug)
                {
                    //throw e;
                }
            }

            return result;
        }


        /// <summary>
        /// Creates QR code using Google chart api.
        /// Returns url of created qr code.
        /// </summary>
        public static String GetQrCodeUrl(String dataToEncode, int width, int height)
        {
            String result = "";

            var url = string.Format("http://chart.apis.google.com/chart?cht=qr&chl={0}&chs={1}x{2}", HttpUtility.UrlEncode(dataToEncode), width, height);
            var mediaService = ApplicationContext.Current.Services.MediaService;

            result = url;

            //get qr code folder
            IMedia qrCodeFolder = null;
            var otherFolder = mediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Other");
            if (otherFolder != null)
            {
                qrCodeFolder = otherFolder.Children().FirstOrDefault(x => x.Name == "QR codes");
                if (qrCodeFolder == null)
                {
                    qrCodeFolder = mediaService.CreateMedia("QR codes", otherFolder, "Folder");
                    mediaService.Save(qrCodeFolder);
                }
            }
            if (qrCodeFolder != null)
            {
                String mediaName = dataToEncode.Substring(dataToEncode.Length > 40 ? dataToEncode.Length - 40 : 0);

                IMedia existingCode = qrCodeFolder.Children().FirstOrDefault(x => x.Name == mediaName);

                if (existingCode == null)
                {
                    try
                    {
                        //create media item
                        IMedia qrCode = mediaService.CreateMedia(mediaName, qrCodeFolder, "Image");

                        //update id
                        mediaService.Save(qrCode);
                        qrCode = qrCodeFolder.Children().FirstOrDefault(x => x.Name == mediaName);

                        //create folder
                        String mediaRootPath = "~/media/";
                        String folderPath = mediaRootPath + qrCode.Id;
                        System.IO.Directory.CreateDirectory(Server.MapPath(folderPath));

                        //download file
                        String fileName = mediaName.ReplaceMany(new char[] { '/', '\\', '\'', '\"', ',', '#' }, '-') + qrCode.Id.ToString() + ".png";
                        String filePath = Server.MapPath(folderPath + "/" + fileName);

                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(url, filePath);

                        //set media item properties
                        System.IO.FileInfo info = new FileInfo(filePath);
                        qrCode.Properties["umbracoExtension"].Value = "png";
                        qrCode.Properties["umbracoBytes"].Value = info.Length.ToString();
                        qrCode.Properties["umbracoFile"].Value = "/media/" + qrCode.Id + "/" + fileName;
                        qrCode.Properties["umbracoWidth"].Value = width.ToString();
                        qrCode.Properties["umbracoHeight"].Value = height.ToString();

                        mediaService.Save(qrCode);

                        result = (String)qrCode.Properties["umbracoFile"].Value;
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error(typeof(IMedia), "Error creating qr code for data: " + dataToEncode, e);
                    }
                }
                else
                {
                    result = (String)existingCode.Properties["umbracoFile"].Value;
                }

            }

            return result;
        }

        

        #region HelperMethods


        static string GetImageFieldUrl(IPublishedContent node, string fieldAlias, string cropName = "")
        {
            var result = "";

            if (node.HasValue(fieldAlias))
            {
                var mediaItem = GetMediaItem(node, fieldAlias);

                result = String.IsNullOrEmpty(cropName)
                    ? GetFileUrl(mediaItem)
                    : mediaItem.GetCropUrl("umbracoFile", cropName);
            }

            return result;
        }


        /// <summary>
        /// WORKAROUND for "Umbraco Core Property Value Converters" package (returns IPublishedContent for media picker)
        /// 
        /// Note: this will not be needed when Value Converters will be incorporated to core
        /// </summary>
        static IPublishedContent GetMediaItem(IPublishedContent node, string fieldAlias)
        {
            IPublishedContent result = null;

            var propertyValue = node.GetPropertyValue(fieldAlias);
            if (propertyValue != null)
            {
                if (propertyValue is IPublishedContent)
                {
                    result = (IPublishedContent)propertyValue;
                }
                else
                {
                    var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
                    result = umbracoHelper.TypedMedia(propertyValue);
                }
            }

            return result;
        }


        /// <summary>
        /// Gets "umbracoFile" as string url. Works both with default file upload and handles Image Cropper format values.
        /// </summary>
        public static string GetFileUrl(IPublishedContent mediaItem)
        {
            var result = "";

            var value = mediaItem.GetPropertyValue("umbracoFile");  //WARNING: for file upload "umbracoFile" stores file url, for image cropper it is custom object

            if (value is JObject)
            {
                var cropInfo = (JObject)value;
                result = cropInfo.GetValue("src").ToString();
            }
            else
            {
                result = value.ToString();
            }

            return result;
        }


        /// <summary>
        /// Gets image url by its id
        /// </summary>
        /// <param name="mediaId"></param>
        /// <param name="cropName"></param>
        /// <returns></returns>
        public static string GetImageUrl(int mediaId, string cropName = "")
        {
            var result = "";

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            var mediaItem = umbracoHelper.TypedMedia(mediaId);
            if (mediaItem != null)
            {
                result = String.IsNullOrEmpty(cropName)
                    ? GetFileUrl(mediaItem)
                    : mediaItem.GetCropUrl("umbracoFile", cropName);
            }

            return result;
        }



        #endregion


        
    }
}
