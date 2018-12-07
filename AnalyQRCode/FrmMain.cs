using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ZXing;
using Spire.Pdf;
using System.Drawing.Imaging;

namespace AnalyQRCode
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        string strPath = Application.StartupPath + "\\Temp\\";//设置临时文件路径

        /// <summary>
        /// 读取二维码
        /// 读取失败，返回空字符串
        /// </summary>
        /// <param name="filename">指定二维码图片位置</param>
        static string Read(string filename)
        {
            BarcodeReader reader = new BarcodeReader();
            reader.Options.CharacterSet = "UTF-8";
            Bitmap map = new Bitmap(filename);
            Result result = reader.Decode(map);
            map.Dispose();//释放图片文件占用资源
            if(result==null)//如果不是二维码图片，则删除
                File.Delete(filename);
            return result == null ? "" : result.Text;
        }
        //识别二维码
        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count > 0)//判断是否存在要对应的图片列表项
            {
                listView2.Items.Clear();//清空对比列表
                string filename, result, flag = "";//要比较的图片文件名\图片对应二维码地址\是否重复
                for (int i = 0; i < listView1.Items.Count; i++)//遍历所有要比较的图片列表
                {
                    filename = listView1.Items[i].Text;//记录当前要比较的图片文件名
                    result = Read(filename);//获取图片对应二维码
                    foreach (ListViewItem item in listView2.Items)//遍历对比列表中的项
                    {
                        if (item.SubItems[1].Text == result)//判断是否有重复的二维码地址
                        {
                            flag = "是(" + item.SubItems[0].Text + ")";//设置重复标识，并提示与哪一项重复
                            break;//跳出循环
                        }
                        else
                            flag = "";//设置不重复标识
                    }
                    if (result != "")//如果存在对应二维码地址，则添加到对比列表中
                    {
                        //使用二维码编号生成列表子项
                        ListViewItem lvItem = new ListViewItem(new string[] { filename.Substring(filename.LastIndexOf('\\') + 1), result.Substring(result.LastIndexOf('\\') + 1), flag });
                        listView2.Items.Add(lvItem);//将列表子项添加到对比列表中
                    }
                }
            }
            else
            {
                MessageBox.Show("请确认存在要识别的二维码图片列表！", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        //选择PDF文档路径
        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                textBox1.Text = openFileDialog1.FileName;
        }
        //提取图片
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                System.Threading.ThreadPool.QueueUserWorkItem(//使用线程池
                     (P_temp) =>
                     {
                         button3.Enabled = false;
                         button1.Enabled = false;
                         PdfDocument doc = new PdfDocument();
                         doc.LoadFromFile(textBox1.Text);

                         #region 使用字典存储所有图片及对应页码
                         Dictionary<Image, string> images = new Dictionary<Image, string>();
                         for (int i = 0; i < doc.Pages.Count; i++)
                         {
                             if (doc.Pages[i].ExtractImages() != null)
                             {
                                 foreach (Image image in doc.Pages[i].ExtractImages())
                                 {
                                     images.Add(image, (i+1).ToString("000"));
                                 }
                             }
                         }
                         #endregion

                         /*
                          * 使用foreach遍历所有图片，并添加到List集合中
                         IList<Image> images = new List<Image>();
                         foreach (PdfPageBase page in doc.Pages)
                         {
                             if (page.ExtractImages() != null)
                             {
                                 foreach (Image image in page.ExtractImages())
                                 {
                                     images.Add(image);
                                 }
                             }
                         }
                         */
                         doc.Close();

                         int index = 1;//图片编号
                         string page, tempPage = "1";//当前页码/上一页页码，为了图片重新编号
                         Image tempImage;//当前图片
                         string imageFileName,imageID;//要保存的图片文件名/图片编号
                         #region 遍历字典中存储的所有图片及对应页码，并保存为图片
                         foreach (var image in images)
                         {
                             page = image.Value;
                             tempImage = image.Key;
                             if (page != tempPage)
                                 index = 1;
                             imageID = index++.ToString("000");
                             imageFileName = String.Format(strPath + "第" + page + "页-{0}.png", imageID);
                             tempImage.Save(imageFileName, ImageFormat.Png);
                             tempImage.Dispose();
                             tempPage = page;
                         }
                         #endregion

                         /*
                          * 遍历List集合中的所有图片
                         foreach (Image image in images)
                         {
                             String imageFileName = String.Format(strPath + "Image-{0}.png", index++.ToString("000"));
                             image.Save(imageFileName, ImageFormat.Png);
                         }
                         */
                         listView1.Items.Clear();//清空文件列表
                         DirectoryInfo dir = new DirectoryInfo(strPath);//获取缓存文件路径
                         FileSystemInfo[] files = dir.GetFiles();//获取文件夹中所有文件
                         foreach (FileInfo file in files)//遍历所有文件
                         {
                             if (file.Extension.ToLower() == ".png")//如果是图片文件
                             {
                                 listView1.Items.Add(file.FullName);//显示文件列表
                             }
                         }
                         button3.Enabled = true;
                         button1.Enabled = true;
                     });
            }
            else
            {
                MessageBox.Show("请选择PDF文档路径！", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button2.Focus();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "当前时间：" + DateTime.Now;//实时显示当前系统时间
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //清空缓存文件
            DirectoryInfo dinfo = new DirectoryInfo(Application.StartupPath + "\\Temp\\");
            foreach (FileInfo f in dinfo.GetFiles())
                f.Delete();
        }
        //列表排序
        private void listView2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (this.listView2.Columns[e.Column].Tag == null)
                this.listView2.Columns[e.Column].Tag = true;
            bool flag = (bool)this.listView2.Columns[e.Column].Tag;
            if (flag)
                this.listView2.Columns[e.Column].Tag = false;
            else
                this.listView2.Columns[e.Column].Tag = true;
            this.listView2.ListViewItemSorter = new ListViewSort(e.Column, this.listView2.Columns[e.Column].Tag);
            this.listView2.Sort();//对列表进行自定义排序  
        }
    }
}
