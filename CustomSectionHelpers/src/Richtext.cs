using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using umbraco.cms.businesslogic.datatype;
using umbraco.editorControls.tinyMCE3;

namespace PG.UmbracoExtensions.CustomSectionHelpers
{
    public static class RichtextEditor
    {
        public static string LoadRichtextScriptsAndCss()
        {
            string RichtextScriptsAndCss = 
                        @"<script src=""/umbraco_client/tinymce3/tiny_mce_src.js?module=gzipmodule&amp;themes=umbraco&amp;plugins=contextmenu,umbracoimg,paste,inlinepopups,table,umbracocss,advlink,umbracoembed,spellchecker,noneditable,umbracomacro,umbracopaste,umbracolink,umbracocontextmenu&amp;languages=en"" type=""text/javascript""></script>
                        <script src=""/umbraco_client/tinymce3/tiny_mce.js"" type=""text/javascript""></script>
                        <style>.mceToolbarExternal {left:30px;}</style>";
            //Note: Deleted this before ?module=: rnd=a7cea67b-df7c-4255-92cd-00d279c31fd0&amp;
            //Don't know what its function is. It seems to be a versionId from an umbraco page.

            //RETURN MVC STRING
            return RichtextScriptsAndCss;
        }

        public static MvcHtmlString InitRichtextEditor(this HtmlHelper helper, string TextAreaId)
        {
            #region Get Tabindex and EditorIndex from ViewData Collection
            //Get TabIndex From ViewData Collection
            int TabIndex = 1;
            if (helper.ViewContext.ViewData["TabIndex"] != null) TabIndex = Convert.ToInt16(helper.ViewContext.ViewData["TabIndex"]);

            //Get EditorIndex from ViewData            
            int EditorIndex = 0;
            if (helper.ViewContext.ViewData["EditorIndex"] != null)
            {
                EditorIndex = Convert.ToInt16(helper.ViewContext.ViewData["EditorIndex"]);

                //And save Plus one
                helper.ViewContext.ViewData["EditorIndex"] = EditorIndex + 1;
            } 
            #endregion

            #region Name Toolbar
            //Name ToolbarId
            string umbRichtextToolbarId = "umbRichtextToolbarId_" + TabIndex.ToString();

            //If Multiple Editors on a single page
            if (EditorIndex > 0) umbRichtextToolbarId += EditorIndex.ToString(); 
            #endregion

            #region Create script out of DataType Config
            //LOAD DATATYPE
            DataTypeDefinition d = DataTypeDefinition.GetDataTypeDefinition(-87);

            //LOAD TINYMCE OBJECT
            TinyMCE _tinymce = (TinyMCE)d.DataType.DataEditor;

            string Initstring = "<script type=\"text/javascript\">tinyMCE.init({";

            //PARSE CONFIGURATION ATTRIBUTES
            int i = 0;
            string AddComma = "";
            foreach (string Key in _tinymce.config.AllKeys)
            {
                bool bvalue = false;
                string value = _tinymce.config.GetValues(i).FirstOrDefault();

                //CHANGE VALUE OF MENUBAR LOCATION
                if (Key == "theme_umbraco_toolbar_location") value = "external"; //Value 'Top' is wrong

                //CHANGE PLUGINS VALUE
                if (Key == "plugins") value = "contextmenu,umbracoimg,paste,inlinepopups,table,umbracocss,advlink,umbracoembed,spellchecker,noneditable,umbracomacro,umbracopaste,umbracolink,umbracocontextmenu"; //Value is wrong

                //PARSE BOOLS AS BOOLS
                if (bool.TryParse(value, out bvalue))
                    Initstring += AddComma + Environment.NewLine + Key + ": " + value;
                else
                    Initstring += AddComma + Environment.NewLine + Key + ": '" + value + "'";

                i++;
                AddComma = ", ";
            }

            //ADD LAST CUSTOM ATTRIBUTES
            Initstring += ", skin: 'umbraco', inlinepopups_skin: 'umbraco', umbraco_toolbar_id: '" + umbRichtextToolbarId + "', elements: '" + TextAreaId + "'});</script>";
            //Note: Seemingly not needed: don't know what the function is of these (links it to a certain page and versionId): theme_umbraco_pageId: '1047', theme_umbraco_versionId: 'a7cea67b-df7c-4255-92cd-00d279c31fd0'

            //RETURN MVC STRING
            return new MvcHtmlString(Initstring); 
            #endregion
        }
    }
}