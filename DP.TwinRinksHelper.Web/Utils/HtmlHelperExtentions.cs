using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public static class HtmlHelperExtentions
{

    public static string ToHtmlTable<T>(this IHtmlHelper helper, IEnumerable<T> list, string id, int? border = null, string tableSyle = null, string headerStyle = null, string rowStyle = null, string alternateRowStyle = null, string tableTitle = null, List<string> highLightKeyWords = null, bool checkForEmpty = false, Func<T, string> selectorGenerator = null, Func<T, string> actionGenerator = null)
    {
        StringBuilder result = new StringBuilder();

        if (checkForEmpty)
        {
            if (list.Count() == 0)
            {
                return string.Empty;
            }
        }

        if (!string.IsNullOrWhiteSpace(tableTitle))
        {
            result.Append("<h3>" + tableTitle + "</h3>");
        }


        string borderStr = null;

        if (border != null)
        {
            borderStr = $" border='{border}'";
        }

        if (string.IsNullOrEmpty(tableSyle))
        {
            result.Append("<table id='" + id + "' class='table table-striped table-hover' " + borderStr + "  >");
        }
        else
        {
            result.Append("<table id='" + id + "' class=\"" + tableSyle + "\">");
        }

        System.Reflection.PropertyInfo[] propertyArray = typeof(T).GetProperties();

        if (selectorGenerator != null)
        {
            result.AppendFormat("<th><input type=\"checkbox\" class='chk-select-all' onclick=\"javascript:$('#" + id + "').find('.chk-select').trigger('click');\" ></th>");
        }

      

        foreach (System.Reflection.PropertyInfo prop in propertyArray)
        {
            if (string.IsNullOrEmpty(headerStyle))
            {
                result.AppendFormat("<th>{0}</th>", prop.Name);
            }
            else
            {
                result.AppendFormat("<th class=\"{0}\">{1}</th>", headerStyle, prop.Name);
            }
        }

        if (actionGenerator != null)
        {
            result.AppendFormat("<th>Actions</th>");
        }

        for (int i = 0; i < list.Count(); i++)
        {
            T item = list.ElementAt(i);

            if (!string.IsNullOrEmpty(rowStyle) && !string.IsNullOrEmpty(alternateRowStyle))
            {
                result.AppendFormat("<tr class=\"{0}\">", i % 2 == 0 ? rowStyle : alternateRowStyle);
            }

            else
            {
                result.AppendFormat("<tr>");
            }


            if (selectorGenerator != null)
            {
                string gId = selectorGenerator(item);
                if (string.IsNullOrWhiteSpace(gId))
                {
                    result.AppendFormat("<th></th>");
                }
                else
                {
                    result.AppendFormat("<th><input type=\"checkbox\" class=\"chk-select\" id='" + selectorGenerator(item) + "'></th>");
                }
            }

            foreach (System.Reflection.PropertyInfo prop in propertyArray)
            {
                object value = prop.GetValue(item, null);

                if (value != null && highLightKeyWords != null)
                {


                    string valueStr = value.ToString();

                    bool hit = false;


                    foreach (string keywrd in highLightKeyWords)
                    {
                        if (valueStr.Contains(keywrd))
                        {
                            hit = true;
                            break;
                        }
                    }

                    if (hit)
                    {
                        result.AppendFormat("<td bgcolor='#FF0000'>{0}</td>", valueStr);
                    }
                    else
                    {
                        result.AppendFormat("<td>{0}</td>", value ?? string.Empty);
                    }
                }
                else
                {

                    result.AppendFormat("<td>{0}</td>", value ?? string.Empty);
                }
            }

            if (actionGenerator != null)
            {
                result.AppendFormat("<td>" + actionGenerator(item) + "</td>");
            }

            result.AppendLine("</tr>");
        }

        result.Append("</table>");

        return result.ToString();
    }

}

