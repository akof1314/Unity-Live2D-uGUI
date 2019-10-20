/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// <see cref="Core.CubismDrawable"/> render sort modes.
    /// </summary>
    public enum CubismSortingMode
    {
        /// <summary>
        /// Offsets by Unity's sorting order from back to front.
        /// </summary>
        BackToFrontOrder,

        /// <summary>
        /// Offsets by Unity's sorting order from front to back.
        /// </summary>
        FrontToBackOrder
    }
}
