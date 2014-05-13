using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PG.UmbracoExtensions.CustomSectionHelpers
{
    public static class Tabviewer
    {
        #region LOAD SCRIPTS AND CSS METHOD
        /// <summary>
        /// Loads the correct scripts and css
        /// </summary>
        /// <param name="LoadRichtextScriptsAndCss">If true also returns richtext editor scripts</param>
        /// <returns></returns>
        public static MvcHtmlString LoadTabviewScriptsAndCss(bool LoadRichtextScriptsAndCss = false)
        {
            string TabviewScriptsAndCss = @"
                    <link href='/umbraco_client/ui/default.css' rel='stylesheet' />
                    <link href='/umbraco_client/menuicon/style.css' rel='stylesheet' />
                    <link href='/umbraco_client/panel/style.css' rel='stylesheet' />
                    <link href='/umbraco_client/propertypane/style.css' rel='stylesheet' />
            
                    <script src='/umbraco_client/tabview/javascript.js' type='text/javascript'></script>
                    <link href='/umbraco_client/tabview/style.css' rel='stylesheet' />
                    <script src='/umbraco_client/scrollingmenu/javascript.js' type='text/javascript'></script>
                    <link href='/umbraco_client/scrollingmenu/style.css' rel='stylesheet' />";

            if (LoadRichtextScriptsAndCss) TabviewScriptsAndCss += RichtextEditor.LoadRichtextScriptsAndCss();

            //RETURN MVC STRING
            return new MvcHtmlString(TabviewScriptsAndCss);
        } 
        #endregion

        #region CREATE TABVIEW
        /// <summary>
        /// Creates the Tabview Container in which the tabs are added
        /// </summary>
        /// <param name="TabItems">Contains a list of strings which will compose the Tab titles</param>
        /// <returns></returns>
        public static MvcTabview CreateTabview(this HtmlHelper htmlHelper, List<string> TabItems)
        {
            string BaseName = "CustomSectionMvcTabs";
            string BaseTabName = "CustomSectionMvcTabs_tab0";

            //Reset TabIndex Variable
            htmlHelper.ViewContext.ViewData.Remove("TabIndex");            

            //Add Container + Header
            string Tabview = String.Format(
                                    @"<div id='{0}' style='height:0px;'>
                                        <div class='header'>
                                            <ul>", 
                                            BaseName);

            for (int ti = 0; ti < TabItems.Count(); ti++)
            {
                int i = ti + 1;
                Tabview += String.Format(

                                    @"<li id=""{0}"" class=""tabOff"">
                                        <a id=""{0}a"" href=""#"" onclick=""setActiveTab('{1}','{0}', {1}_tabs); return false;"">
                                            <span><nobr>{2}</nobr></span>
                                        </a>
                                    </li>", 

                                    BaseTabName + i.ToString(), 
                                    BaseName, 
                                    TabItems[ti]);                                                                    
            }

            Tabview += String.Format(
                                    @"</ul>
                                </div>
                                <div id='' class='tabpagecontainer'>");

            htmlHelper.ViewContext.Writer.Write(Tabview);
            return new MvcTabview(htmlHelper.ViewContext);
        }        
        #endregion

        #region ADD TAB
        /// <summary>
        /// Adds a tab to the Tabview. You must specify here if you want Richtext Editors on the tab. If so, The tab will load a menubar. 
        /// </summary>
        /// <param name="NrOfRichtextEditors">How much Richtext editors you need on this tab. Foreach editor a separate menubar will be added.</param>
        /// <returns></returns>
        public static MvcTab AddTab(this HtmlHelper htmlHelper, int NrOfRichtextEditors = 0)
        {
            #region Get Current Tab + Save to Temp Data (For use within View disposable tab {})
            int TabIndex = 1;
            if (htmlHelper.ViewContext.ViewData["TabIndex"] != null)
            {
                TabIndex = Convert.ToInt16(htmlHelper.ViewContext.ViewData["TabIndex"]) + 1;
                htmlHelper.ViewContext.ViewData["TabIndex"] = TabIndex;
            }
            else
                htmlHelper.ViewContext.ViewData.Add("TabIndex", TabIndex);
            
            #endregion

            #region Base variables
            string BaseTabName = "CustomSectionMvcTabs_tab0" + TabIndex.ToString();
            string umbRichtextToolbarId = "umbRichtextToolbarId_" + TabIndex.ToString();

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

                //Save Editor Index State (For multiple Editors)
                if (NrOfRichtextEditors > 1)
                {
                    htmlHelper.ViewContext.ViewData.Remove("EditorIndex");
                    htmlHelper.ViewContext.ViewData["EditorIndex"] = 1;
                }
            } 
            #endregion

            #region Add Html context
            //Width of menu for scrolling arrow purposes
            string ScrollingMenuWidth = "100";
            if (NrOfRichtextEditors > 0) { ScrollingMenuWidth = "1200"; }

            string TabPage = String.Format(
                        @"<div id=""{0}layer"" class=""tabpage"">                        
                            <div class='menubar'>
                                <span id=""{0}layer_menu"" style=""display:inline-block;width:400px;"">
                                    <table id=""{0}layer_menu_tableContainer"">
                                        <tr id=""{0}layer_menu_tableContainerRow"">
                                            <td id=""{0}layer_menu_tableContainerLeft"">
                                                <img class=""editorArrow"" align=""absMiddle"" onMouseOut=""this.className = &#39;editorArrow&#39;; scrollStop();"" onMouseOver=""this.className = &#39;editorArrowOver&#39;; scrollR(&#39;{0}layer_menu_sl&#39;,&#39;{0}layer_menu_slh&#39;,{2});"" src=""/umbraco_client/scrollingmenu/images/arrawBack.gif"" style=""border-width:0px;height:20px;width:7px;"" />
                                            </td>
                                            <td id=""{0}layer_menu_tableContainerButtons"">
                                                <div id=""{0}layer_menu_slh"" class=""slh"" style=""height:26px;width:370px;"">
                                                    <script>RegisterScrollingMenuButtons('{0}layer_menu', '{0}layer_save');</script>
                                                    <div id=""{0}layer_menu_sl"" style=""top:0px;left:0px;height:26px;width:1576px;"" class=""sl"">
                                                        <nobr id=""{0}layer_menu_nobr""></nobr>
                                                        <input id=""{0}_save"" name=""{0}_save"" class=""editorIcon"" type=""image"" style=""height:23px;width:22px;border:0px;"" alt=""Save"" src=""/umbraco/images/editor/save.gif"" onmousedown=""this.className='editorIconDown'"" onmouseup=""this.className='editorIconOver'"" onmouseout=""this.className='editorIcon'"" onmouseover=""this.className='editorIconOver'"" title=""Save"" />
                                                        {1}            
                                                    </div>
                                                </div>                                            
                                            </td>
                                            <td id=""{0}layer_menu_tableContainerRight"">
                                                <img class=""editorArrow"" align=""absMiddle"" onMouseOut=""this.className = &#39;editorArrow&#39;; scrollStop();"" onMouseOver=""this.className = &#39;editorArrowOver&#39;; scrollL(&#39;{0}layer_menu_sl&#39;,&#39;{0}layer_menu_slh&#39;,{2});"" src=""/umbraco_client/scrollingmenu/images/arrowForward.gif"" style=""border-width:0px;height:20px;width:7px;"" />
                                            </td>
                                        </tr>
                                    </table>
                                </span>
                            </div>
                            <div id=""{0}layer_contentlayer"" class=""tabpagescrollinglayer""  style=""height:-50px;width:400px"">
                            <div class=""tabpageContent"" style=""padding:0 10px;"">",
                            BaseTabName,
                            sAddTinyMceMenu,
                            ScrollingMenuWidth);

            htmlHelper.ViewContext.Writer.Write(TabPage);
            return new MvcTab(htmlHelper.ViewContext); 
            #endregion
        }        
        #endregion
    }

    #region TABVIEW DISPOSABLES
    /// <summary>
    /// This closes the Tab and is why you can use it with a using() {} statement
    /// </summary>        
    public class MvcTab : IDisposable
    {
        private readonly TextWriter _writer;
        public MvcTab(ViewContext viewContext)
        {
            _writer = viewContext.Writer;
        }

        public void Dispose()
        {           
            this._writer.Write("</div></div></div>");
        }
    }

    /// <summary>
    /// This closes the Tabview and is why you can use it with a using() {} statement. 
    /// </summary>    
    public class MvcTabview : IDisposable
    {
        private readonly TextWriter _writer;
        public MvcTabview(ViewContext viewContext)
        {
            _writer = viewContext.Writer;
        }

        public void Dispose()
        {
            string TabFooter = @"</div><div class='footer'><div class='status'><h2></h2></div></div>
                                <input type='hidden' value='CustomSectionMvcTabs_tab01' id='CustomSectionMvcTabs_activetab' name='CustomSectionMvcTabs_activetab'>
                                <script type='text/javascript'>
                                    var CustomSectionMvcTabs_tabs = new Array();
                                    $('.tabpage').each(function (index) { CustomSectionMvcTabs_tabs[index] = $(this).attr('id').replace('layer', '');});
                                    setActiveTab('CustomSectionMvcTabs', 'CustomSectionMvcTabs_tab01', CustomSectionMvcTabs_tabs);
                                    jQuery(document).ready(function () { resizeTabView(CustomSectionMvcTabs_tabs, 'CustomSectionMvcTabs'); });
                                    jQuery(window).resize(function () { resizeTabView(CustomSectionMvcTabs_tabs, 'CustomSectionMvcTabs'); });
                                </script>
                            </div>";

            this._writer.Write(TabFooter);
        }
    } 
    #endregion
}