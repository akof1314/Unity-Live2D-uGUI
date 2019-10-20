/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using Live2D.Cubism.Core;
using UnityEngine;
using UnityEngine.UI;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Wrapper for drawing <see cref="CubismDrawable"/>s.
    /// </summary>
    [ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    public sealed class CubismRenderer : MaskableGraphic
    {
        /// <summary>
        /// <see cref="LocalSortingOrder"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _localSortingOrder;

        /// <summary>
        /// Local sorting order.
        /// </summary>
        public int LocalSortingOrder
        {
            get
            {
                return _localSortingOrder;
            }
            set
            {
                // Return early if same value given.
                if (value == _localSortingOrder)
                {
                    return;
                }


                // Store value.
                _localSortingOrder = value;


                // Apply it.
                ApplySorting();
            }
        }

        /// <summary>
        /// <see cref="MainTexture"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Texture2D _mainTexture;

        /// <summary>
        /// <see cref="MeshRenderer"/>'s main texture.
        /// </summary>
        public Texture2D MainTexture
        {
            get { return _mainTexture; }
            set
            {
                // Return early if same value given and main texture is valid.
                if (value == _mainTexture && _mainTexture != null)
                {
                    return;
                }


                // Store value.
                _mainTexture = (value != null)
                    ? value
                    : Texture2D.whiteTexture;


                // Apply it.
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get { return MainTexture; }
        }

        /// <summary>
        /// Meshes.
        /// </summary>
        /// <remarks>
        /// Double buffering dynamic meshes increases performance on mobile, so we double-buffer them here.
        /// </remarks>

        private Mesh[] Meshes { get; set; }

        /// <summary>
        /// Index of front buffer mesh.
        /// </summary>
        private int FrontMesh { get; set; }

        /// <summary>
        /// Index of back buffer mesh..
        /// </summary>
        private int BackMesh { get; set; }

        /// <summary>
        /// <see cref="UnityEngine.Mesh"/>.
        /// </summary>
        public Mesh Mesh
        {
            get { return Meshes[FrontMesh]; }
        }


        #region Interface For CubismRenderController

        /// <summary>
        /// <see cref="SortingMode"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismSortingMode _sortingMode;

        /// <summary>
        /// Sorting mode.
        /// </summary>
        private CubismSortingMode SortingMode
        {
            get { return _sortingMode; }
            set { _sortingMode = value; }
        }


        /// <summary>
        /// <see cref="RenderOrder"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _renderOrder;

        /// <summary>
        /// Sorting mode.
        /// </summary>
        private int RenderOrder
        {
            get { return _renderOrder; }
            set { _renderOrder = value; }
        }


        /// <summary>
        /// <see cref="Opacity"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private float _opacity;

        /// <summary>
        /// Opacity.
        /// </summary>
        private float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }


        /// <summary>
        /// Buffer for vertex colors.
        /// </summary>
        private Color[] VertexColors { get; set; }


        /// <summary>
        /// Allows tracking of what vertex data was updated last swap.
        /// </summary>
        private SwapInfo LastSwap { get; set; }

        /// <summary>
        /// Allows tracking of what vertex data will be swapped.
        /// </summary>
        private SwapInfo ThisSwap { get; set; }
        
        
        /// <summary>
        /// Swaps mesh buffers.
        /// </summary>
        /// <remarks>
        /// Make sure to manually call this method in case you changed the <see cref="Color"/>.
        /// </remarks>
        public void SwapMeshes()
        {
            // Perform internal swap.
            BackMesh = FrontMesh;
            FrontMesh = (FrontMesh == 0) ? 1 : 0;


            var mesh = Meshes[FrontMesh];


            // Update colors.
            Meshes[BackMesh].colors = VertexColors;
            
            
            // Update swap info.
            LastSwap = ThisSwap;


            ResetSwapInfoFlags();
            

            // Apply swap.
            SetVerticesDirty();
        }

        private List<List<Vector3>> m_LastVertexPositions;

        protected override void Awake()
        {
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);
            if (canvasRenderer.cull)
            {
                return;
            }

            if (update == CanvasUpdate.PreRender)
            {
                var mesh = Meshes[FrontMesh];
                canvasRenderer.SetMesh(mesh);
            }
        }

        /// <summary>
        /// Updates visibility.
        /// </summary>
        public void UpdateVisibility()
        {
            if (LastSwap.DidBecomeVisible)
            {
                transform.localScale = Vector3.one;
            }
            else if (LastSwap.DidBecomeInvisible)
            {
                transform.localScale = Vector3.zero;
            }


            ResetVisibilityFlags();
        }
        
        
        /// <summary>
        /// Updates render order. 
        /// </summary>
        public void UpdateRenderOrder()
        {
            if (LastSwap.NewRenderOrder)
            {
                ApplySorting();
            }

            
            ResetRenderOrderFlag();
        }

        /// <summary>
        /// Updates sorting mode.
        /// </summary>
        /// <param name="newSortingMode">New sorting mode.</param>
        internal void OnControllerSortingModeDidChange(CubismSortingMode newSortingMode)
        {
            SortingMode = newSortingMode;


            ApplySorting();
        }


        /// <summary>
        /// Sets the opacity.
        /// </summary>
        /// <param name="newOpacity">New opacity.</param>
        internal void OnDrawableOpacityDidChange(float newOpacity)
        {
            Opacity = newOpacity;


            ApplyVertexColors();
        }

        /// <summary>
        /// Updates render order.
        /// </summary>
        /// <param name="newRenderOrder">New render order.</param>
        internal void OnDrawableRenderOrderDidChange(int newRenderOrder)
        {
            RenderOrder = newRenderOrder;


            SetNewRenderOrder();
        }

        private bool IsMeshVertexPositionsNotChanged(List<Vector3> vertexPositions, Vector3[] newVertexPositions)
        {
            bool notChanged = false;
            if (vertexPositions != null && newVertexPositions.Length == vertexPositions.Count)
            {
                for (int i = 0; i < vertexPositions.Count; i++)
                {
                    if (vertexPositions[i] != newVertexPositions[i])
                    {
                        break;
                    }

                    if (i == vertexPositions.Count - 1)
                    {
                        notChanged = true;
                    }
                }
            }

            return notChanged;
        }

        /// <summary>
        /// Sets the <see cref="UnityEngine.Mesh.vertices"/>.
        /// </summary>
        /// <param name="newVertexPositions">Vertex positions to set.</param>
        internal bool OnDrawableVertexPositionsDidChange(Vector3[] newVertexPositions)
        {
            if (m_LastVertexPositions == null)
            {
                m_LastVertexPositions = new List<List<Vector3>>();
                m_LastVertexPositions.Add(new List<Vector3>());
                m_LastVertexPositions.Add(new List<Vector3>());
            }

            Mesh.GetVertices(m_LastVertexPositions[FrontMesh]);
            var frontNotChanged = IsMeshVertexPositionsNotChanged(m_LastVertexPositions[FrontMesh], newVertexPositions);
            if (frontNotChanged)
            {
                int back = (FrontMesh == 0) ? 1 : 0;
                Meshes[back].GetVertices(m_LastVertexPositions[back]);
                var backNotChanged = IsMeshVertexPositionsNotChanged(m_LastVertexPositions[back], newVertexPositions);
                if (backNotChanged)
                {
                    return false;
                }
            }

            var mesh = Mesh;

            // Apply positions and update bounds.
            mesh.vertices = newVertexPositions;

            mesh.RecalculateBounds();


            // Set swap flag.
            SetNewVertexPositions();
            return true;
        }

        /// <summary>
        /// Sets visiblity.
        /// </summary>
        /// <param name="newVisibility">New visibility.</param>
        internal void OnDrawableVisiblityDidChange(bool newVisibility)
        {
            // Set swap flag if visible.
            if (newVisibility)
            {
                BecomeVisible();
            }
            else
            {
                BecomeInvisible();
            }
        }


        /// <summary>
        /// Sets model opacity.
        /// </summary>
        /// <param name="newModelOpacity">Opacity to set.</param>
        internal void OnModelOpacityDidChange(float newModelOpacity)
        {
            var col = color;
            col.a = newModelOpacity;
            color = col;
        }

        #endregion


        /// <summary>
        /// Applies sorting.
        /// </summary>
        private void ApplySorting()
        {
            // Sort by order.
            int newOrder = ((SortingMode == CubismSortingMode.BackToFrontOrder)
                ? (RenderOrder + LocalSortingOrder)
                : -(RenderOrder + LocalSortingOrder));
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                transform.SetSiblingIndex(newOrder);
                return;
            }
#endif
            var lastOrder = transform.GetSiblingIndex();
            if (lastOrder != newOrder)
            {
                transform.SetSiblingIndex(newOrder);
            }
        }

        /// <summary>
        /// Uploads mesh vertex colors.
        /// </summary>
        public void ApplyVertexColors()
        {
            var vertexColors = VertexColors;
            var colorN = color;


            colorN.a *= Opacity;


            for (var i = 0; i < vertexColors.Length; ++i)
            {
                vertexColors[i] = colorN;
            }


            // Set swap flag.
            SetNewVertexColors();
        }
        

        /// <summary>
        /// Initializes the mesh if necessary.
        /// </summary>
        private void TryInitializeMesh()
        {
            // Only create mesh if necessary.
            // HACK 'Mesh.vertex > 0' makes sure mesh is recreated in case of runtime instantiation.
            if (Meshes != null && Mesh.vertexCount > 0)
            {
                return;
            }


            // Create mesh for attached drawable.
            var drawable = GetComponent<CubismDrawable>();


            if (Meshes == null)
            {
                Meshes = new Mesh[2];
            }


            for (var i = 0; i < 2; ++i)
            {
                var mesh = new Mesh
                {
                    name = drawable.name,
                    vertices = drawable.VertexPositions,
                    uv = drawable.VertexUvs,
                    triangles = drawable.Indices
                };

                mesh.MarkDynamic();
                mesh.RecalculateBounds();


                // Store mesh.
                Meshes[i] = mesh;
            }
        }

        /// <summary>
        /// Initializes vertex colors.
        /// </summary>
        private void TryInitializeVertexColor()
        {
            var mesh = Mesh;
            

            VertexColors = new Color[mesh.vertexCount];


            for (var i = 0; i < VertexColors.Length; ++i)
            {
                VertexColors[i] = color;
                VertexColors[i].a *= Opacity;
            }
        }

        /// <summary>
        /// Initializes the main texture if possible.
        /// </summary>
        private void TryInitializeMainTexture()
        {
            if (MainTexture == null)
            {
                MainTexture = null;
            }


            SetMaterialDirty();
        }
        
        
        /// <summary>
        /// Initializes components if possible.
        /// </summary>
        public void TryInitialize()
        {
            TryInitializeMesh();
            TryInitializeVertexColor();
            TryInitializeMainTexture();


            ApplySorting();
        }

        #region Swap Info
        
        /// <summary>
        /// Sets <see cref="NewVertexPositions"/>.
        /// </summary>
        private void SetNewVertexPositions()
        {
            var swapInfo = ThisSwap;
            swapInfo.NewVertexPositions = true;
            ThisSwap = swapInfo;
        }


        /// <summary>
        /// Sets <see cref="NewVertexColors"/>.
        /// </summary>
        private void SetNewVertexColors()
        {
            var swapInfo = ThisSwap;
            swapInfo.NewVertexColors = true;
            ThisSwap = swapInfo;
        }


        /// <summary>
        /// Sets <see cref="DidBecomeVisible"/> on visible.
        /// </summary>
        private void BecomeVisible()
        {
            var swapInfo = ThisSwap;
            swapInfo.DidBecomeVisible = true;
            ThisSwap = swapInfo;
        }


        /// <summary>
        /// Sets <see cref="DidBecomeInvisible"/> on invisible.
        /// </summary>
        private void BecomeInvisible()
        {
            var swapInfo = ThisSwap;
            swapInfo.DidBecomeInvisible = true;
            ThisSwap = swapInfo;
        }
        

        /// <summary>
        /// Sets <see cref="SetNewRenderOrder"/>.
        /// </summary>
        private void SetNewRenderOrder()
        {
            var swapInfo = ThisSwap;
            swapInfo.NewRenderOrder = true;
            ThisSwap = swapInfo;
        }
        
        
        /// <summary>
        /// Resets flags.
        /// </summary>
        private void ResetSwapInfoFlags()
        {
            var swapInfo = ThisSwap;
            swapInfo.NewVertexColors = false;
            swapInfo.NewVertexPositions = false;
            swapInfo.DidBecomeVisible = false;
            swapInfo.DidBecomeInvisible = false;
            ThisSwap = swapInfo;
        }
        

        /// <summary>
        /// Reset visibility flags.
        /// </summary>
        private void ResetVisibilityFlags()
        {
            var swapInfo = LastSwap;
            swapInfo.DidBecomeVisible = false;
            swapInfo.DidBecomeInvisible = false;
            LastSwap = swapInfo;
        }
        
        
        /// <summary>
        /// Reset render order flag.
        /// </summary>
        private void ResetRenderOrderFlag()
        {
            var swapInfo = LastSwap;
            swapInfo.NewRenderOrder = false;
            LastSwap = swapInfo;
        }
        
        
        /// <summary>
        /// Allows tracking of <see cref="Mesh"/> data changed on a swap.
        /// </summary>
        private struct SwapInfo
        {
            /// <summary>
            /// Vertex positions were changed.
            /// </summary>
            public bool NewVertexPositions { get; set; }

            /// <summary>
            /// Vertex colors were changed.
            /// </summary>
            public bool NewVertexColors { get; set; }
            
            /// <summary>
            /// Visibility were changed to visible.
            /// </summary>
            public bool DidBecomeVisible { get; set; }

            /// <summary>
            /// Visibility were changed to invisible.
            /// </summary>
            public bool DidBecomeInvisible { get; set; }
            
            /// <summary>
            /// Render order were changed.
            /// </summary>
            public bool NewRenderOrder { get; set; }
        }

        #endregion
    }
}