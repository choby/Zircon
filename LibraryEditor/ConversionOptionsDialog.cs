using System;
using System.Windows.Forms;

namespace LibraryEditor
{
    public partial class ConversionOptionsDialog : Form
    {
        public LibraryConversionOptions Options { get; private set; }

        public ConversionOptionsDialog()
        {
            InitializeComponent();
            SelectDefaultOptions();
            UpdateAtlasControls();
            UpdateSummary();
        }

        private void SelectDefaultOptions()
        {
            SelectComboBoxDefault(_individualRuntimeComboBox, 0);
            SelectComboBoxDefault(_runtimeComboBox, 0);
            SelectComboBoxDefault(_compressionComboBox, 0);
        }

        private static void SelectComboBoxDefault(ComboBox comboBox, int selectedIndex)
        {
            if (comboBox.Items.Count == 0 || comboBox.SelectedIndex >= 0)
                return;

            comboBox.SelectedIndex = Math.Min(selectedIndex, comboBox.Items.Count - 1);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (_openDialog.ShowDialog(this) != DialogResult.OK)
                return;

            foreach (string fileName in _openDialog.FileNames)
            {
                if (!_fileListBox.Items.Contains(fileName))
                    _fileListBox.Items.Add(fileName);
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            while (_fileListBox.SelectedItems.Count > 0)
                _fileListBox.Items.Remove(_fileListBox.SelectedItems[0]);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (_fileListBox.Items.Count == 0)
            {
                MessageBox.Show(this, "请至少选择一个要转换的资源库。", "转换资源库");
                DialogResult = DialogResult.None;
                return;
            }

            string[] fileNames = new string[_fileListBox.Items.Count];
            for (int i = 0; i < _fileListBox.Items.Count; i++)
                fileNames[i] = _fileListBox.Items[i].ToString();

            Options = new LibraryConversionOptions
            {
                FileNames = fileNames,
                BuildAtlasMetadata = _buildAtlasCheckBox.Checked,
                BuildShadowAtlasMetadata = _buildAtlasCheckBox.Checked && _buildShadowAtlasCheckBox.Checked,
                BuildOverlayAtlasMetadata = _buildAtlasCheckBox.Checked && _buildOverlayAtlasCheckBox.Checked,
                StorePngSourceImages = false,
                AtlasGroupImageCount = _buildAtlasCheckBox.Checked ? (int)_atlasGroupNumeric.Value : 0,
                AtlasPageSize = _buildAtlasCheckBox.Checked ? (int)_atlasPageSizeNumeric.Value : 2048,
                IndividualRuntimePreference = GetSelectedIndividualRuntimePreference(),
                RuntimePreference = GetSelectedRuntimePreference(),
                ContainerCompression = GetSelectedContainerCompression()
            };
        }

        private ZlRuntimeTexturePreference GetSelectedRuntimePreference()
        {
            switch (_runtimeComboBox.SelectedIndex)
            {
                case 0:
                default:
                    return ZlRuntimeTexturePreference.Bc7;
                case 1:
                    return ZlRuntimeTexturePreference.Bgra32;
            }
        }

        private ZlRuntimeTexturePreference GetSelectedIndividualRuntimePreference()
        {
            return _individualRuntimeComboBox.SelectedIndex switch
            {
                0 => ZlRuntimeTexturePreference.Source,
                1 => ZlRuntimeTexturePreference.Dxt1,
                2 => ZlRuntimeTexturePreference.Bc7,
                _ => ZlRuntimeTexturePreference.None,
            };
        }

        private ZlContainerCompression GetSelectedContainerCompression()
        {
            switch (_compressionComboBox.SelectedIndex)
            {
                case 0:
                default:
                    return ZlContainerCompression.DeflateBest;
                case 1:
                    return ZlContainerCompression.DeflateFast;
                case 2:
                    return ZlContainerCompression.None;
            }
        }

        private void BuildAtlasCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAtlasControls();
        }

        private void SummaryControl_Changed(object sender, EventArgs e)
        {
            UpdateSummary();
        }

        private void UpdateAtlasControls()
        {
            bool enabled = _buildAtlasCheckBox.Checked;
            _runtimeComboBox.Enabled = enabled;
            _buildShadowAtlasCheckBox.Enabled = enabled;
            _buildOverlayAtlasCheckBox.Enabled = enabled;
            _atlasGroupNumeric.Enabled = enabled;
            _atlasPageSizeNumeric.Enabled = enabled;
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (_summaryLabel == null)
                return;

            string individualText = _individualRuntimeComboBox.SelectedIndex switch
            {
                0 => "源格式：尽可能保留源纹理编码，适合由 WTL 转换的运行时资源库。",
                1 => "DXT1：适合不透明图块或不含 Alpha 通道的图像。",
                2 => "BC7：为独立 DX11 纹理提供最佳画质。",
                _ => "独立运行时纹理：无；依赖图集或 PNG 解码。"
            };

            string atlasText;
            if (_buildAtlasCheckBox.Checked)
            {
                atlasText = _runtimeComboBox.SelectedIndex == 0
                    ? "图集：使用 BC7 页面以获得最快的 DX11 图像渲染路径。"
                    : "图集：使用 BGRA 页面；体积较大，但便于检查。";

                if (_buildShadowAtlasCheckBox.Checked || _buildOverlayAtlasCheckBox.Checked)
                    atlasText += " 包含阴影层和叠加层。";

                if (_atlasGroupNumeric.Value > 0)
                    atlasText += $" 每 {_atlasGroupNumeric.Value:N0} 张图像拆分一次。";
            }
            else
            {
                atlasText = "图集：已禁用；更适合页面局部性较差的地图或图块资源库。";
            }

            string compressionText = _compressionComboBox.SelectedIndex switch
            {
                1 => "压缩：Deflate 快速压缩；转换更快，文件略大。",
                2 => "压缩：不压缩；加载和保存最快，文件最大。",
                _ => "压缩：Deflate 最佳压缩；推荐的默认体积与速度平衡方案。"
            };

            _summaryLabel.Text = $"{individualText}{Environment.NewLine}{Environment.NewLine}{atlasText}{Environment.NewLine}{Environment.NewLine}{compressionText}";
        }
    }
}
