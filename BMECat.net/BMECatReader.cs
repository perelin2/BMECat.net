/*
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
    internal class BMECatReader
    {
        public static ProductCatalog Load(Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new IllegalStreamException("Cannot read from stream");
            }

            XmlDocument doc = new XmlDocument();
            stream.Seek(0, SeekOrigin.Begin);
            doc.Load(stream);
           
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.DocumentElement.OwnerDocument.NameTable);
            nsmgr.AddNamespace("xsi", "http://www.bmecat.org/bmecat/1.2/bmecat_new_catalog");

            //string version = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/@version");
            //if (version != "2005")
            //{
            //    throw new Exception("Only version 2005 is currently supported");
            //}

            ProductCatalog retval = new ProductCatalog();

            foreach(XmlNode languageNode in doc.DocumentElement.SelectNodes("/BMECAT/HEADER/CATALOG/LANGUAGE"))
            {
                LanguageCodes language = default(LanguageCodes).FromString(languageNode.InnerText);
                retval.Languages.Add(language);
            }

            retval.CatalogId = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/CATALOG/CATALOG_ID");
            retval.CatalogVersion = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/CATALOG/CATALOG_VERSION");
            retval.CatalogName = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/CATALOG/CATALOG_NAME");
            retval.GenerationDate = XmlUtils.nodeAsDateTime(doc.DocumentElement, "/BMECAT/HEADER/CATALOG/GENERATION_DATE");
            retval.Currency = default(CurrencyCodes).FromString(XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/CATALOG/CURRENCY"));
            foreach (XmlNode priceFlagNode in doc.DocumentElement.SelectNodes("/BMECAT/HEADER/CATALOG/PRICE_FLAG"))
            {
                retval.PriceFlags.Add(new PriceFlag() {
                    PriceFlagActive = priceFlagNode.InnerText,
                    PriceFlagType= priceFlagNode.Attributes.GetNamedItem("type").Value
                });
            }
// end catalog


// start Buyer
            retval.Buyer.Id = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ID");
//            retval.Buyer.IdType = doc.DocumentElement.SelectNodes("/BMECAT/HEADER/BUYER/BUYER_ID").Item(1).Attributes.GetNamedItem("type").Value;
            retval.Buyer.IdType = XmlUtils.attributeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ID", "type");
            retval.Buyer.Name = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_NAME");
            retval.Buyer.AddressContact = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ADDRESS_CONTACT");
            retval.Buyer.AddressStreet = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ADDRESS_STREET");
            retval.Buyer.AddressZIP = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ADDRESS_ZIP");
            retval.Buyer.AddressCity = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ADDRESS_CITY");
            retval.Buyer.AddressCountry = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/BUYER/BUYER_ADDRESS_COUNTRY");

// end Buyer

// start Supplier

            retval.Supplier.Id = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/SUPPLIER_ID");
            retval.Supplier.IdType = XmlUtils.attributeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/SUPPLIER_ID", "type");
            retval.Supplier.Name = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/SUPPLIER_NAME");
            retval.Supplier.AddressStreet = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/STREET");
            retval.Supplier.AddressZIP = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/ZIP");
            retval.Supplier.AddressCity = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/CITY");
            retval.Supplier.AddressCountry = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/COUNTRY");
            retval.Supplier.Phone = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/PHONE");
            retval.Supplier.Fax = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/FAX");
            retval.Supplier.EMail = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/EMAIL");
            retval.Supplier.Url = XmlUtils.nodeAsString(doc.DocumentElement, "/BMECAT/HEADER/SUPPLIER/ADDRESS/URL");
            // end Supplier

            XmlNodeList productNodes = doc.DocumentElement.SelectNodes("/BMECAT/T_NEW_CATALOG/PRODUCT");
            Parallel.ForEach(productNodes.Cast<XmlNode>(),
                             (XmlNode productNode) =>
            {
                string _productMode = XmlUtils.nodeAsString(productNode, "@mode");
                Product product = new Product()
                {
                    No = XmlUtils.nodeAsString(productNode, "./SUPPLIER_PID"),
                    DescriptionShort = XmlUtils.nodeAsString(productNode, "./PRODUCT_DETAILS/DESCRIPTION_SHORT"),
                    DescriptionLong = XmlUtils.nodeAsString(productNode, "./PRODUCT_DETAILS/DESCRIPTION_LONG"),
                    EANCode = XmlUtils.nodeAsString(productNode, "./PRODUCT_DETAILS/EAN"),
                    Stock = XmlUtils.nodeAsInt(productNode, "./PRODUCT_DETAILS/STOCK"),
                    OrderUnit = default(QuantityCodes).FromString(XmlUtils.nodeAsString(productNode, "./PRODUCT_ORDER_DETAILS/ORDER_UNIT")),
                    ContentUnit = default(QuantityCodes).FromString(XmlUtils.nodeAsString(productNode, "./PRODUCT_ORDER_DETAILS/CONTENT_UNIT")),
                    Currency = default(CurrencyCodes).FromString(XmlUtils.nodeAsString(productNode, "./PRODUCT_PRICE_DETAILS/PRODUCT_PRICE/PRICE_CURRENCY")),
                    VAT = XmlUtils.nodeAsInt(productNode, "./PRODUCT_PRICE_DETAILS/PRODUCT_PRICE/TAX")
                };

                decimal? _netPrice = XmlUtils.nodeAsDecimal(productNode, "./PRODUCT_PRICE_DETAILS/PRODUCT_PRICE/PRICE_AMOUNT");
                if (_netPrice.HasValue)
                {
                    product.NetPrice = _netPrice.Value;
                }

                retval.Products.Add(product);
            });

            return retval;
        } // !Load()


        public static ProductCatalog Load(string filename)
        {
            if (!System.IO.File.Exists(filename))
            {
                throw new FileNotFoundException();
            }

            return Load(new FileStream(filename, FileMode.Open, FileAccess.Read));
        } // !Load()
    }
}
