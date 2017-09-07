﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BMECat.net
{
    public class BMECatWriter
    {
        public ProductCatalog Catalog { get; private set; }
        private XmlTextWriter Writer { get; set; }


        public void Save(ProductCatalog catalog, Stream stream)
        {
            if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new IllegalStreamException("Cannot write to stream");
            }

            long streamPosition = stream.Position;

            this.Catalog = catalog;
            this.Writer = new XmlTextWriter(stream, Encoding.UTF8);
            Writer.Formatting = Formatting.Indented;
            Writer.WriteStartDocument();
            Writer.WriteDocType("BMECAT",null, "bmecat_new_catalog_1_2.dtd",null);

            #region XML-Kopfbereich
            Writer.WriteStartElement("BMECAT");
            Writer.WriteAttributeString("version", "1.2");
//            Writer.WriteAttributeString("xmlns", "xsi", null, "http://www.bmecat.org/bmecat/2005fd");
            #endregion // !XML-Kopfbereich

            #region Header
            Writer.WriteStartElement("HEADER");
            _writeOptionalElementString(Writer, "GENERATOR_INFO", this.Catalog.GeneratorInfo);

            Writer.WriteStartElement("CATALOG");
            foreach (LanguageCodes _language in this.Catalog.Languages)
            {
                Writer.WriteElementString("LANGUAGE", _language.EnumToString());
            }
            Writer.WriteElementString("CATALOG_ID", this.Catalog.CatalogId); // Pflichtfeld
            Writer.WriteElementString("CATALOG_VERSION", this.Catalog.CatalogVersion); // Pflichtfeld
            _writeOptionalElementString(Writer, "CATALOG_NAME", this.Catalog.CatalogName);
            _writeDateTime(elementName: "GENERATION_DATE", date: this.Catalog.GenerationDate);
            Writer.WriteElementString("CURRENCY", this.Catalog.Currency.EnumToString());
            foreach(PriceFlag priceflag in this.Catalog.PriceFlags)
            {
                Writer.WriteStartElement("PRICE_FLAG");
                Writer.WriteAttributeString("type", priceflag.PriceFlagType);
                Writer.WriteString(priceflag.PriceFlagActive);
                Writer.WriteEndElement(); // PRICE_FLAG
            }
            _writeTransport(Writer, this.Catalog.Transport);
            Writer.WriteEndElement(); // !CATALOG

            if (this.Catalog.Buyer != null)
            {
                Writer.WriteStartElement("BUYER");
                if (!String.IsNullOrEmpty(this.Catalog.Buyer.Id))
                {
                    Writer.WriteStartElement("BUYER_ID");
                    Writer.WriteAttributeString("type", "buyer_specific");
                    Writer.WriteString(this.Catalog.Buyer.Id);
                    Writer.WriteEndElement(); // !BUYER_ID
                }
                _writeOptionalElementString(Writer, "BUYER_NAME", this.Catalog.Buyer.Name);

                Writer.WriteStartElement("ADDRESS");
                Writer.WriteAttributeString("type", "buyer");
                Writer.WriteElementString("NAME", this.Catalog.Buyer.Name);
                if (!String.IsNullOrEmpty(this.Catalog.Buyer.ContactName))
                {
                    Writer.WriteElementString("CONTACT", this.Catalog.Buyer.ContactName);
                }
                Writer.WriteEndElement(); // !ADDRESS

                Writer.WriteEndElement(); // !BUYER
            }

            Writer.WriteEndElement(); // !HEADER
            #endregion // !Header

            #region PRODUCTS
            Writer.WriteStartElement("T_NEW_CATALOG");
            foreach(Product product in this.Catalog.Products)
            {
                Writer.WriteStartElement("PRODUCT");
                Writer.WriteAttributeString("mode", "new");
                _writeOptionalElementString(Writer, "SUPPLIER_PID", product.No);

                Writer.WriteStartElement("PRODUCT_DETAILS");
                _writeOptionalElementString(Writer, "DESCRIPTION_SHORT", product.DescriptionShort);
                _writeOptionalElementString(Writer, "DESCRIPTION_LONG", product.DescriptionLong);
                _writeOptionalElementString(Writer, "EAN", product.EANCode);
                _writeOptionalElementString(Writer, "STOCK", String.Format("{0}", product.Stock));
                Writer.WriteEndElement(); // !PRODUCT_DETAILS

                Writer.WriteStartElement("PRODUCT_ORDER_DETAILS");
                if (product.OrderUnit != QuantityCodes.Unknown)
                {
                    _writeOptionalElementString(Writer, "ORDER_UNIT", product.OrderUnit.EnumToString());
                }
                if (product.ContentUnit != QuantityCodes.Unknown)
                {
                    _writeOptionalElementString(Writer, "CONTENT_UNIT", product.ContentUnit.EnumToString());
                }
                Writer.WriteEndElement(); // !PRODUCT_ORDER_DETAILS

                Writer.WriteStartElement("PRODUCT_PRICE_DETAILS");
                Writer.WriteStartElement("PRODUCT_PRICE");
                Writer.WriteAttributeString("price_type", "net_list");
                Writer.WriteElementString("PRICE_AMOUNT", _formatDecimal(product.NetPrice));
                if (product.Currency != CurrencyCodes.Unknown)
                {
                    Writer.WriteElementString("PRICE_CURRENCY", product.Currency.EnumToString());
                }
                else
                {
                    Writer.WriteElementString("PRICE_CURRENCY", this.Catalog.Currency.EnumToString());
                }
                Writer.WriteElementString("TAX", _formatDecimal(1.0 * product.VAT / 100.0));

                Writer.WriteEndElement(); // !PRODUCT_PRICE
                Writer.WriteEndElement(); // !PRODUCT_PRICE_DETAILS

                Writer.WriteEndElement(); // !PRODUCT
            }
            Writer.WriteEndElement(); // !T_NEW_CATALOG
            #endregion // !ARTICLES


            Writer.WriteEndElement(); // !BMECAT
            Writer.WriteEndDocument();
            Writer.Flush();

            stream.Seek(streamPosition, SeekOrigin.Begin);
        } // !Save()


        public void Save(ProductCatalog catalog, string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            Save(catalog, fs);
            fs.Flush();
            fs.Close();
        } // !Save()


        private void _writeTransport(XmlTextWriter writer, TransportConditions transportCondition)
        {
            if (transportCondition == null)
            {
                return;
            }

            writer.WriteStartElement("TRANSPORT");
            writer.WriteElementString("INCOTERM", transportCondition.Incoterm.EnumToString());
            _writeOptionalElementString(Writer, "LOCATION", transportCondition.Location);
            _writeOptionalElementString(Writer, "TRANSPORT_REMARK", transportCondition.Remark);
            writer.WriteEndElement(); // !TRANSPORT
        } // !_writeTransport()


        private void _writeDateTime(string elementName, string typeAttribute = "", DateTime? date = null)
        {
            Writer.WriteStartElement(elementName);
            if (!string.IsNullOrEmpty(typeAttribute))
            {
                Writer.WriteAttributeString("type", typeAttribute);
            }
            /*
            Writer.WriteElementString("DATE", date.ToString("yyyy-dd-MM"));
            Writer.WriteElementString("TIME", date.ToString("hh:mm"));
            Writer.WriteElementString("TIMEZONE", date.ToString("zzz"));
            */
                if (date.HasValue)
            {
                Writer.WriteString(date.Value.ToString("yyyy-MM-ddThh:mm:sszzz"));
            }
            Writer.WriteEndElement();
        } // !_writeDateTime()


        private string _formatDecimal(double value, int numDecimals = 2)
        {
            return _formatDecimal((decimal)value, numDecimals);
        } // !_formatDecimal()


        private string _formatDecimal(float value, int numDecimals = 2)
        {
            return _formatDecimal((decimal)value, numDecimals);
        } // !_formatDecimal()


        private string _formatDecimal(decimal value, int numDecimals = 2)
        {
            string formatString = "0.";
            for (int i = 0; i < numDecimals; i++)
            {
                formatString += "0";
            }

            return value.ToString(formatString).Replace(",", ".");
        } // !_formatDecimal()


        private void _writeOptionalElementString(XmlTextWriter writer, string tagName, string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                writer.WriteElementString(tagName, value);
            }
        } // !_writeOptionalElementString()
    }
}