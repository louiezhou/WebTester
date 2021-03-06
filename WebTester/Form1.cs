﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.XPath;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using WebTester.Entity;

namespace WebTester
{
    public partial class Form1 : Form
    {
        private readonly List<HeaderEntity> _headerList = new List<HeaderEntity>();
        

        public Form1()
        {
            InitializeComponent();
            //_headerList.Add(new HeaderEntity(){Key = "Host", Value = "www.baidu.com"});
        }

        private void btnSetHeader_Click(object sender, EventArgs e)
        {
            new AddHeader(_headerList).ShowDialog();
        }

        private void btnGetHtml_Click(object sender, EventArgs e)
        {
            if (txtURL.Text.IsNullOrEmpty())
            {
                ShowMessage("URL不能为空！");
                return;
            }

            if (!txtURL.Text.Trim().StartsWith("http"))
            {
                txtURL.Text = @"http://" + txtURL.Text.Trim();
            }
            HttpItem hi = new HttpItem();
            hi.URL = txtURL.Text;
            Type hiType = hi.GetType();
            foreach (var header in _headerList)
            {
                if (header.Key.IsNullOrEmpty())
                {
                    continue;
                }
                var propertyInfo = hiType.GetProperty(header.Key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(hi, header.Value, null);
                }
                else
                {
                    hi.Header.Add(header.Key, header.Value);
                }
            }

            if (rbPost.Checked)
            {
                hi.Postdata = txtPostData.Text;
                hi.Method = "POST";
            }

            if (cbRedirect.Checked)
            {
                hi.Allowautoredirect = true;
            }
            HttpResult result = new HttpHelper().GetHtml(hi);
            if (result.StatusCode != HttpStatusCode.OK)
            {
                txtHtml.Text = $@"错误码：{(int) result.StatusCode} {result.StatusCode}\r\n错误描述：{result.Html}";
            }
            else
            {
                txtURL.Text = result.ResponseUri;
                txtHtml.Text = result.Html;
            }
        }

        private void ShowMessage(string msg)
        {
            MessageBox.Show(msg, @"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnAnalysis_Click(object sender, EventArgs e)
        {
            if (txtExpression.Text.IsNullOrEmpty())
            {
                ShowMessage("解析表达式不能为空！");
                return;
            }

            txtResult.Text = "";
            if (cbShowCount.Checked)
            {
                txtResult.Text = $@"";
            }
            try
            {
                List<ResultEntity> list;
                list = rbXpath.Checked ? GetNodeValue(txtHtml.Text, txtExpression.Text) : GetList(txtHtml.Text, txtExpression.Text);
                StringBuilder sb = new StringBuilder();
                if (cbShowCount.Checked)
                {
                    sb.AppendLine($@"共获取节点{list.Count}个");
                    sb.AppendLine("-----------------------------------------------------------------");
                }
                list.ForEach(x =>
                {
                    sb.AppendLine(x.Value);
                    if (cbShowPath.Checked)
                    {
                        sb.AppendLine(x.Path);
                    }
                    sb.AppendLine("-----------------------------------------------------------------");
                });
                txtResult.Text = sb.ToString();
            }
            catch (Exception exception)
            {
                txtResult.Text = $@"{exception.Message}
{exception.StackTrace}";
            }
            
        }

        private List<ResultEntity> GetNodeValue(string html, string xpath)
        {
            var list = new List<ResultEntity>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var root = doc.DocumentNode;
            HtmlNodeNavigator navigator = (HtmlNodeNavigator)root.CreateNavigator();
            var nodes = navigator.Select(xpath);
;           foreach (HtmlNodeNavigator node in nodes)
            {
                list.Add(new ResultEntity(){Value = node.Value, Path = node.CurrentNode.XPath});
            }

            return list;
        }

        private List<ResultEntity> GetList(string json, string jpath)
        {
            JObject o = JObject.Parse(json);
            var tokens = o.SelectTokens(jpath);
            return tokens.Select(x => new ResultEntity(){Value = x.ToString(), Path = x.Path}).ToList();
        }
    }
}
