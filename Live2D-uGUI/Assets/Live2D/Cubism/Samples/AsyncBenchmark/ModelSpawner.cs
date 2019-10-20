/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Rendering;
using System.Collections.Generic;
using Live2D.Cubism.Framework.Tasking;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;


namespace Live2D.Cubism.Samples.AsyncBenchmark
{
    /// <summary>
    /// Spawns models for benchmarking.
    /// </summary>
    public sealed class ModelSpawner : MonoBehaviour
    {
        /// <summary>
        /// <see cref="CubismModel"/> prefab to spawn.
        /// </summary>
        [SerializeField]
        public GameObject ModelPrefab;


        /// <summary>
        /// UI component representing current model count.
        /// </summary>
        [SerializeField]
        public Text ModelCountUi;


        /// <summary>
        /// Model instances.
        /// </summary>
        private List<GameObject> Instances { get; set; }

        #region Interface for UI Elements

        /// <summary>
        /// Adds a new instance.
        /// </summary>
        public void IncreaseInstances()
        {
            if (ModelPrefab == null)
            {
                return;
            }

            // Spawn new instance.
            var instance = Instantiate(ModelPrefab);

            var tf = ModelCountUi.transform.parent.parent.GetChild(2);
            var size = tf.GetComponent<RectTransform>().rect;

            var rt = instance.transform.GetComponent<RectTransform>();
            rt.SetParent(tf, false);
            rt.anchorMin = Vector2.up;
            rt.anchorMax = Vector2.up;
            rt.sizeDelta = new Vector2(200f, 400f);
            rt.localScale *= 0.5f;

            int num = Instances.Count;
            var num2 = Mathf.FloorToInt(size.width / (rt.sizeDelta.x / 2));
            var num3 = num / num2;
            var num4 = num % num2;
            rt.anchoredPosition = new Vector2(50f + rt.sizeDelta.x / 2 * num4, -100f - rt.sizeDelta.y / 2 * num3);
            //rt.localScale *= 300f;

            // Register instance and update UI.
            Instances.Add(instance);


            // Make sure to assign a unique sorting order to the instance.
            //instance.GetComponent<CubismRenderController>().SortingOrder = Instances.Count;


            // Update UI.
            ModelCountUi.text = Instances.Count.ToString();
        }

        /// <summary>
        /// Removes an instance.
        /// </summary>
        public void DecreaseInstances()
        {
            // Return early if there's nothing to decrease.
            if (Instances.Count == 0)
            {
                return;
            }


            // Remove last instance and update UI.
            DestroyImmediate(Instances[Instances.Count - 1]);
            Instances.RemoveAt(Instances.Count - 1);


            ModelCountUi.text = Instances.Count.ToString();
        }

        #endregion

        #region Unity Event Handling

        /// <summary>
        /// Called by Unity. Initializes fields.
        /// </summary>
        private void Start()
        {
            Instances = new List<GameObject>();
        }

        #endregion
    }
}
