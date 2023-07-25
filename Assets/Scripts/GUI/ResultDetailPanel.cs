#nullable enable
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{    
    [RequireComponent(typeof (CanvasGroup))]
    public class ResultDetailPanel : MonoBehaviour
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        // Clockwise rotation of preview, in 90 degree increments
        public enum PreviewRotation
        {
            ZERO = 0,         // Not rotated
            NINETY = 1,       // 90 degree clockwise
            ONE_EIGHTY = 2,   // 180 degree clockwise
            TWO_SEVENTY = 3   // 270 degree clockwise
        }
        
        [SerializeField] private Image? detailImage;
        [SerializeField] private Image? coordRefImage;
        [SerializeField] private PreviewRotation currentRotation = PreviewRotation.ZERO;
        private Texture2D? texture = null;

        private static int GetPos(int x, int y, int z, int sizeX, int sizeY)
        {
            return x + y * sizeX + z * sizeX * sizeY;
        }

        private static int GetRotatedPos(int x, int y, int z, int sizeX, int sizeY, PreviewRotation rotation)
        {
            (int rotX, int rotY) = rotation switch
            {
                PreviewRotation.ZERO           => (x            ,             y),
                PreviewRotation.NINETY         => (sizeY - 1 - y,             x),
                PreviewRotation.ONE_EIGHTY     => (sizeX - 1 - x, sizeY - 1 - y),
                PreviewRotation.TWO_SEVENTY    => (y            , sizeX - 1 - x),

                _                              => (x            ,             y)
            };

            if (rotation == PreviewRotation.ZERO || rotation == PreviewRotation.ONE_EIGHTY)
                return rotX + rotY * sizeX + z * sizeX * sizeY;
            else
                return rotX + rotY * sizeY + z * sizeY * sizeX;
        }

        public static (int[] pixels, int texX, int texY) RenderPreview(int sizeX, int sizeY, int sizeZ,
                byte[] srcState, int[] colors, PreviewRotation rotation = PreviewRotation.ZERO)
        {
            bool swapXYSize = rotation == PreviewRotation.NINETY || rotation == PreviewRotation.TWO_SEVENTY;

            var state = new byte[srcState.Length];
            for (int z = 0; z < sizeZ; z++) for (int y = 0; y < sizeY; y++) for (int x = 0; x < sizeX; x++)
            {
                // Flip X when sampling from source
                int srcPos = GetPos(sizeX - 1 - x, y, z, sizeX, sizeY);
                int dstPos = GetRotatedPos(x, y, z, sizeX, sizeY, rotation);

                state[dstPos] = srcState[srcPos];
            }

            return MarkovJunior.Graphics.Render(state, swapXYSize ? sizeY : sizeX, swapXYSize ? sizeX : sizeY, sizeZ, colors, 6, 0);
        }

        // Returns whether the current preview is 2d
        private bool UpdateDetailPreview(bool initRotation)
        {
            // Remove previous texture
            texture = null;

            var exporter = GetComponentInParent<ExporterScreen>();
            
            if (exporter is not null)
            {
                var prev = exporter.GetPreviewData();

                int sizeX = prev.sizeX;
                int sizeY = prev.sizeY;
                int sizeZ = prev.sizeZ;

                bool is2d = sizeZ == 1;

                if (initRotation)
                {
                    // Use 90 deg rotation for 3d to keep the orientation consistency
                    currentRotation = is2d ? ResultDetailPanel.PreviewRotation.ZERO : ResultDetailPanel.PreviewRotation.NINETY;
                }

                // Update Preview Image
                var (pixels, texX, texY) = RenderPreview(prev.sizeX, prev.sizeY, prev.sizeZ, prev.state, prev.colors, currentRotation);
                var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, texX, texY);
                //tex.filterMode = FilterMode.Point;
                // Update sprite
                var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
                detailImage!.sprite = sprite;
                detailImage!.SetNativeSize();

                texture = tex;

                return is2d;
            }

            return false;
        }

        public byte[] GetCoordRefData(int coordRefSize)
        {
            var coordRefStates = new byte[coordRefSize * coordRefSize * coordRefSize];

            // Origin, use color #1
            coordRefStates[GetPos(0, 0, 0, coordRefSize, coordRefSize)] = 1;

            for (int i = 2;i < coordRefSize;i++)
            {
                // X axis, use color #2
                coordRefStates[GetPos(i, 0, 0, coordRefSize, coordRefSize)] = 2;
                // Y axis, use color #3
                coordRefStates[GetPos(0, i, 0, coordRefSize, coordRefSize)] = 3;
                // Z axis, use color #4
                coordRefStates[GetPos(0, 0, i, coordRefSize, coordRefSize)] = 4;
            }

            return coordRefStates;
        }

        private void UpdateCoordRef(bool is2d)
        {
            var coordRefSize = 6;
            int sizeX = coordRefSize;
            int sizeY = coordRefSize;
            var sizeZ = is2d ? 1 : coordRefSize;

            var colors = new int[]{
                0x000000,                            // #0, Black (will be treated as air if is2d)
                ColorConvert.GetOpaqueRGB(0xFFFFFF), // #1, White
                ColorConvert.GetOpaqueRGB(0xFF0000), // #2, Red
                ColorConvert.GetOpaqueRGB(0x00FF00), // #3, Green
                ColorConvert.GetOpaqueRGB(0x0000FF)  // #4, Blue
            };

            // Update Preview Image
            var (pixels, texX, texY) = RenderPreview(sizeX, sizeY, sizeZ, GetCoordRefData(coordRefSize), colors, currentRotation);
            var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, texX, texY);
            //tex.filterMode = FilterMode.Point;
            // Update sprite
            var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
            coordRefImage!.sprite = sprite;
            coordRefImage!.SetNativeSize();
        }

        public void RotateClockwise()
        {
            currentRotation = (PreviewRotation)(((int) currentRotation + 1) % 4);

            bool is2d = UpdateDetailPreview(false);
            UpdateCoordRef(is2d);
        }

        public void RotateCounterClockwise()
        {
            currentRotation = (PreviewRotation)(((int) currentRotation + 4 - 1) % 4);

            bool is2d = UpdateDetailPreview(false);
            UpdateCoordRef(is2d);
        }

        public void Save()
        {
            var exporter = GetComponentInParent<ExporterScreen>();
            var savePath = exporter.GetExportNames();

            if (texture != null && savePath != null)
            {
                var folder = new DirectoryInfo(savePath.Value.dir);

                if (folder.Exists)
                {
                    var bytes = texture.EncodeToPNG();
                    var fileName = folder.FullName + SP + $"{savePath.Value.name}.png";

                    File.WriteAllBytes(fileName, bytes);
                    Debug.Log($"Preview image saved to {fileName}");
                }
                else
                {
                    Debug.LogWarning("Target folder doesn't exist!");
                }
            }
        }

        public void Show()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            bool is2d = UpdateDetailPreview(true);
            UpdateCoordRef(is2d);
        }

        public void Hide()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}