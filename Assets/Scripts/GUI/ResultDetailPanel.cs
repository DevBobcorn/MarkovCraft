#nullable enable
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MarkovCraft
{    
    [RequireComponent(typeof (CanvasGroup))]
    public class ResultDetailPanel : MonoBehaviour
    {
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
        [SerializeField] private TMPro.TMP_InputField? imageExportNameInput;
        
        private int sizeX = 0;
        private int sizeY = 0;
        private int sizeZ = 0;
        private int[] state = { };
        private int[] colors = { };
        HashSet<int> airIndices = new();

        private PreviewRotation currentRotation = PreviewRotation.ZERO;
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
                int[] srcState, int[] colors, HashSet<int> airIndices, PreviewRotation rotation = PreviewRotation.ZERO)
        {
            bool swapXYSize = rotation == PreviewRotation.NINETY || rotation == PreviewRotation.TWO_SEVENTY;

            var state = new int[srcState.Length];
            for (int z = 0; z < sizeZ; z++) for (int y = 0; y < sizeY; y++) for (int x = 0; x < sizeX; x++)
            {
                // Flip X when sampling from source
                int srcPos = GetPos(sizeX - 1 - x, y, z, sizeX, sizeY);
                int dstPos = GetRotatedPos(x, y, z, sizeX, sizeY, rotation);

                state[dstPos] = srcState[srcPos];
            }

            return MarkovJunior.Graphics.Render(state, swapXYSize ? sizeY : sizeX, swapXYSize ? sizeX : sizeY, sizeZ, colors, airIndices, 6, 0);
        }

        // Returns whether the current preview is 2d
        private bool UpdateDetailPreview(bool initRotation)
        {
            // Remove previous texture
            texture = null;

            bool is2d = sizeZ == 1;

            if (initRotation)
            {
                // Use 90 deg rotation for 3d to keep the orientation consistency
                currentRotation = is2d ? PreviewRotation.ZERO : PreviewRotation.NINETY;
            }

            // Update Preview Image
            var (pixels, texX, texY) = RenderPreview(sizeX, sizeY, sizeZ, state, colors, airIndices, currentRotation);
            var tex = MarkovJunior.Graphics.CreateTexture2D(pixels, texX, texY);
            //tex.filterMode = FilterMode.Point;
            // Update sprite
            var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(tex.width / 2, tex.height / 2));
            detailImage!.sprite = sprite;
            detailImage!.SetNativeSize();

            texture = tex;

            return is2d;
        }

        public int[] GetCoordRefData(int coordRefSize)
        {
            var coordRefStates = new int[coordRefSize * coordRefSize * coordRefSize];

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
                0x000000,                            // #0, Black, Fully Transparent, Air
                ColorConvert.GetOpaqueRGB(0xFFFFFF), // #1, White
                ColorConvert.GetOpaqueRGB(0xFF0000), // #2, Red
                ColorConvert.GetOpaqueRGB(0x00FF00), // #3, Green
                ColorConvert.GetOpaqueRGB(0x0000FF)  // #4, Blue
            };

            // #0 is air
            var airIndices = new HashSet<int>(){ 0 };

            // Update Preview Image
            var (pixels, texX, texY) = RenderPreview(sizeX, sizeY, sizeZ, GetCoordRefData(coordRefSize), colors, airIndices, currentRotation);
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
            var savePath = imageExportNameInput!.text;

            if (GameScene.CheckFileName(savePath) && (texture is not null))
            {
                var fileInfo = new FileInfo(savePath);
                if (fileInfo.Directory.Exists) // If the folder exists
                {
                    var bytes = texture.EncodeToPNG();

                    File.WriteAllBytes(savePath, bytes);
                    Debug.Log($"Preview image saved to {savePath}");
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

            var manipulator = GetComponentInParent<ResultManipulatorScreen>();
            if (manipulator is not null)
            {
                var baseName = manipulator.GetDefaultBaseName();
                imageExportNameInput!.text = MarkovGlobal.GetDataFile($"{baseName}.png");

                (sizeX, sizeY, sizeZ, state, colors, airIndices) = manipulator.GetResult()!.GetPreviewData();
            }

            bool is2d = UpdateDetailPreview(true);
            UpdateCoordRef(is2d);
        }

        public void UpdateSizeAndState(int sizeX, int sizeY, int sizeZ, int[] state)
        {
            // Update preview data
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.sizeZ = sizeZ;
            this.state = state;
            // Update preview image
            UpdateDetailPreview(false);
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