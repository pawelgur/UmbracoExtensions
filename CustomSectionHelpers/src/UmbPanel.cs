using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PG.UmbracoExtensions.CustomSectionHelpers
{
    public static class UmbPanel
    {
        #region LOAD SCRIPTS AND CSS METHOD
        /// <summary>
        /// Loads the correct scripts and css
        /// </summary>
        /// <param name="LoadRichtextScriptsAndCss">If true also returns richtext editor scripts</param>
        /// <returns></returns>
        public static MvcHtmlString LoadPanelScriptsAndCss(bool LoadRichtextScriptsAndCss = false)
        {
            string PanelScriptsAndCss = @"
                    <link href='/umbraco_client/ui/default.css' rel='stylesheet' />
                    <link href='/umbraco_client/menuicon/style.css' rel='stylesheet' />
                    <link href='/umbraco_client/panel/style.css' rel='stylesheet' />
                    <link href='/umbraco_client/propertypane/style.css' rel='stylesheet' />
                    <script src='/umbraco_client/panel/javascript.js' type='text/javascript'></script>
                    <script src='/umbraco_client/application/jquery/jquery.cookie.js' type='text/javascript'></script>

                    <script src='/umbraco_client/scrollingmenu/javascript.js' type='text/javascript'></script>
                    <link href='/umbraco_client/scrollingmenu/style.css' rel='stylesheet' />";

            if (LoadRichtextScriptsAndCss) PanelScriptsAndCss += RichtextEditor.LoadRichtextScriptsAndCss();


            //RETURN MVC STRING
            return new MvcHtmlString(PanelScriptsAndCss);
        }
        #endregion

        #region ADD PANEL TO PAGE
        /// <summary>
        /// Creates a regular Umbraco Panel when you don't need tabs. 
        /// </summary>
        /// <param name="Title">The title in the header box</param>
        /// <param name="AddSaveButton">Optional: whether you need a save button or not</param>
        /// <param name="NrOfRichtextEditors">Specify how menu richtext editors you need. It will then load a separate menubar per editor</param>
        /// <returns></returns>
        public static MvcUmbPanel CreateUmbPanel(this HtmlHelper htmlHelper, string Title, bool AddSaveButton = true, int NrOfRichtextEditors = 0)
        {
            string umbRichtextToolbarId = "umbRichtextToolbarId_1";

            if (NrOfRichtextEditors > 0) AddSaveButton = true;

            string sAddTinyMceMenu = "";
            if (NrOfRichtextEditors > 0)
            {
                //Adds a tinymceMenuBar for each richtexteditor
                for (int i = 0; i < NrOfRichtextEditors; i++)
                {
                    string ToolbarId = umbRichtextToolbarId;
                    if (NrOfRichtextEditors > 1) ToolbarId += (i + 1).ToString();
                    sAddTinyMceMenu += "<img class=\"editorIconSplit\" src=\"/umbraco_client/menuicon/images/split.gif\" style=\"height:21px;border:0px;\" /><div class=\"tinymceMenuBar\" id=\"" + ToolbarId + "\"></div>";
                }
            }

            string SaveButton = "";
            if (AddSaveButton)            
                SaveButton = @"<input id=""body_UmbracoPanel_save"" name=""body_UmbracoPanel_save"" class=""editorIcon"" type=""image"" style=""height:23px;width:22px;border:0px;"" alt=""Save"" src=""/umbraco/images/editor/save.gif"" onmousedown=""this.className='editorIconDown'"" onmouseup=""this.className='editorIconOver'"" onmouseout=""this.className='editorIcon'"" onmouseover=""this.className='editorIconOver'"" title=""Save"" />";
            

            //Width of menu for scrolling arrow purposes
            string ScrollingMenuWidth = "100";
            if (NrOfRichtextEditors > 0) { ScrollingMenuWidth = "1200"; }

            //Add Container
            string PanelContainer = String.Format(
                                    @"<div id=""body_UmbracoPanel"" class=""panel"" style=""width:100%;"">
                                        <div class=""boxhead""><h2 id=""body_UmbracoPanelLabel"">{0}</h2></div>
                                        <div class=""boxbody"">
                                            <div id=""body_UmbracoPanel_menubackground"" class=""menubar_panel"">
                                                <span id=""body_UmbracoPanel_menu"">

                                                    

                                                    <table id=""body_UmbracoPanel_menu_tableContainer"">
                                                        <tr id=""body_UmbracoPanel_menu_tableContainerRow"">
                                                            <td id=""body_UmbracoPanel_menu_tableContainerLeft"">
                                                                <img class=""editorArrow"" align=""absMiddle"" onMouseOut=""this.className = &#39;editorArrow&#39;; scrollStop();"" onMouseOver=""this.className = &#39;editorArrowOver&#39;; scrollR(&#39;body_UmbracoPanel_menu_sl&#39;,&#39;body_UmbracoPanel_menu_slh&#39;,{3});"" src=""/umbraco_client/scrollingmenu/images/arrawBack.gif"" style=""border-width:0px;height:20px;width:7px;"" />
                                                            </td>
                                                            <td id=""body_UmbracoPanel_menu_tableContainerButtons"">
                                                                <div id=""body_UmbracoPanel_menu_slh"" class=""slh"" style=""height:26px;width:370px;"">
                                                                    <script>RegisterScrollingMenuButtons('body_UmbracoPanel_menu', 'body_UmbracoPanel_save');</script>
                                                                    <div id=""body_UmbracoPanel_menu_sl"" style=""top:0px;left:0px;height:26px;width:1576px;"" class=""sl"">
                                                                        <nobr id=""body_UmbracoPanel_menu_nobr""></nobr>
                                                                        {1} {2}            
                                                                    </div>
                                                                </div>                                            
                                                            </td>
                                                            <td id=""body_UmbracoPanel_menu_tableContainerRight"">
                                                                <img class=""editorArrow"" align=""absMiddle"" onMouseOut=""this.className = &#39;editorArrow&#39;; scrollStop();"" onMouseOver=""this.className = &#39;editorArrowOver&#39;; scrollL(&#39;body_UmbracoPanel_menu_sl&#39;,&#39;body_UmbracoPanel_menu_slh&#39;,{3});"" src=""/umbraco_client/scrollingmenu/images/arrowForward.gif"" style=""border-width:0px;height:20px;width:7px;"" />
                                                            </td>
                                                        </tr>
                                                    </table>

                                                </span>
                                            </div>
                                            <div id=""body_UmbracoPanel_content"" class=""content"">
                                            <div class=""innerContent"" id=""body_UmbracoPanel_innerContent"">",
                                        Title,
                                        SaveButton,
                                        sAddTinyMceMenu,
                                        ScrollingMenuWidth);

            htmlHelper.ViewContext.Writer.Write(PanelContainer);

            //Add EditorIndex to the ViewDataCollection so the right menubar is coupled to the right textarea
            if (NrOfRichtextEditors > 1)
            {
                htmlHelper.ViewContext.ViewData.Remove("EditorIndex");
                htmlHelper.ViewContext.ViewData.Add("EditorIndex", 1);
            }

            return new MvcUmbPanel(htmlHelper.ViewContext);
        }
    } 
        #endregion

    #region PANEL DISPOSABLE
    /// <summary>
    /// This closes the Panel and is why you can use it with a using() {} statement
    /// </summary>
    public class MvcUmbPanel : IDisposable
    {
        private readonly TextWriter _writer;
        public MvcUmbPanel(ViewContext viewContext)
        {
            _writer = viewContext.Writer;
        }

        public void Dispose()
        {
            string TabFooter = @"</div>
                                </div>
                            </div>
                            <div class=""boxfooter"">
                                <div class=""statusBar""><h2></h2></div>
                            </div>
                            <script type='text/javascript'>
                                jQuery(document).ready(function() {jQuery(window).load(function(){ resizePanel('body_UmbracoPanel', true,true); }) });
                            </script>
                        </div>";



            this._writer.Write(TabFooter);
        }
    } 
    #endregion
}