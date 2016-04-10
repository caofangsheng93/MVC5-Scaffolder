﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Happy.Scaffolding.MVC.Scaffolders
{
    public static class  ModelDisplayExtensions
    {
        public static string GetDisplayName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> expression)
        {
            Type type = typeof(TModel);
            IEnumerable<string> propertyList;

            //unless it's a root property the expression NodeType will always be Convert
            switch (expression.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var ue = expression.Body as UnaryExpression;
                    propertyList = (ue != null ? ue.Operand : null).ToString().Split(".".ToCharArray()).Skip(1); //don't use the root property
                    break;
                default:
                    propertyList = expression.Body.ToString().Split(".".ToCharArray()).Skip(1);
                    break;
            }

            //the propert name is what we're after
            string propertyName = propertyList.Last();
            //list of properties - the last property name
            string[] properties = propertyList.Take(propertyList.Count() - 1).ToArray();

            Expression expr = null;
            foreach (string property in properties)
            {
                PropertyInfo propertyInfo = type.GetProperty(property);
                expr = Expression.Property(expr, type.GetProperty(property));
                type = propertyInfo.PropertyType;
            }

            DisplayAttribute attr = (DisplayAttribute)type.GetProperty(propertyName).GetCustomAttributes(typeof(DisplayAttribute), true).SingleOrDefault();

            // Look for [MetadataType] attribute in type hierarchy
            // http://stackoverflow.com/questions/1910532/attribute-isdefined-doesnt-see-attributes-applied-with-metadatatype-class
            if (attr == null)
            {
                MetadataTypeAttribute metadataType = (MetadataTypeAttribute)type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).FirstOrDefault();
                if (metadataType != null)
                {
                    var property = metadataType.MetadataClassType.GetProperty(propertyName);
                    if (property != null)
                    {
                        attr = (DisplayAttribute)property.GetCustomAttributes(typeof(DisplayNameAttribute), true).SingleOrDefault();
                    }
                }
            }
            //To support translations call attr.GetName() instead of attr.Name
            return (attr != null) ? attr.GetName() : String.Empty;
        }
    }



    public static class AttributeHelper
    {
        public static string GetDisplayName(object obj, string propertyName)
        {
            if (obj == null) return null;
            return GetDisplayName(obj.GetType(), propertyName);

        }

        public static string GetDisplayName(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property == null) return null;

            return GetDisplayName(property);
        }

        public static string GetDisplayName(PropertyInfo property)
        {
            var attrName = GetAttributeDisplayName(property);
            if (!string.IsNullOrEmpty(attrName))
                return attrName;

            var metaName = GetMetaDisplayName(property);
            if (!string.IsNullOrEmpty(metaName))
                return metaName;

            return property.Name.ToString();
        }

        private static string GetAttributeDisplayName(PropertyInfo property)
        {
            var atts = property.GetCustomAttributes(
                typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), true);
            if (atts.Length == 0)
                return null;
            return (atts[0] as System.ComponentModel.DataAnnotations.DisplayAttribute).Name;
        }
        
        private static string GetMetaDisplayName(PropertyInfo property)
        {
            var atts = property.DeclaringType.GetCustomAttributes(
                typeof(MetadataTypeAttribute), true);
            if (atts.Length == 0)
                return null;

            var metaAttr = atts[0] as MetadataTypeAttribute;
            var metaProperty =
                metaAttr.MetadataClassType.GetProperty(property.Name);
            if (metaProperty == null)
                return null;
            return GetAttributeDisplayName(metaProperty);
        }
        //---------------------------------------------------------------------
        public static bool GetRequired(object obj, string propertyName)
        {
            if (obj == null) return false;
            return GetRequired(obj.GetType(), propertyName);

        }

        public static bool GetRequired(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property == null) return false;

            return GetRequired(property);
        }
        public static bool GetRequired(PropertyInfo property)
        {
            var required = GetAttributeRequired(property);
            if (required)
                return required;

            required = GetMetaRequired(property);
            return required;
            
        }
        private static bool GetAttributeRequired(PropertyInfo property) {
            var atts = property.GetCustomAttributes(
                typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true);
            if (atts.Length == 0)
                return false;
            return true;
        }
        private static bool GetMetaRequired(PropertyInfo property) {
            var atts = property.DeclaringType.GetCustomAttributes(
                    typeof(MetadataTypeAttribute), true);
            if (atts.Length == 0)
                return false;

            var metaAttr = atts[0] as MetadataTypeAttribute;
            var metaProperty =
                metaAttr.MetadataClassType.GetProperty(property.Name);
            if (metaProperty == null)
                return false;
            return GetAttributeRequired(metaProperty);
        }
    }
}
