﻿using System;
using System.Collections;
using System.IO;
using System.Diagnostics;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils;
using BepInEx.IL2CPP.Utils.Collections;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// Game Specific
using RG;
using Chara;
using CharaCustom;
using System.Collections.Generic;
using System.Linq;

namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        public static void MakeClothesDropdown(CvsC_Clothes clothesControl)
        {
            ChaControl charaControl = clothesControl.chaCtrl;
            string characterName = charaControl.gameObject.name;

            // index according to enum ChaFileDefine.ClothesKind
            int kind = clothesControl.SNo;
            GameObject clothesPiece = GetClothes(charaControl, kind);

            // Stored textures for this kind (piece) of clothing
            CharacterContent characterContent = CharactersLoaded[characterName];
            Dictionary<int, MaterialContent> dicMaterials;
            if (kind == 0) dicMaterials = characterContent.clothesTop;
            else if (kind == 1) dicMaterials = characterContent.clothesBottom;
            else return;

            // Create one button for each material
            renderList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            // Dropdown for materials (already created in RG_MaterialModUI
            Dropdown clothesDropdown = clothesSettingsGroup.GetComponentInChildren<Dropdown>();
            clothesDropdown.ClearOptions();
            Il2CppSystem.Collections.Generic.List<string> dropdownOptions = new Il2CppSystem.Collections.Generic.List<string>();
            dropdownOptions.Clear();

            // Getting Texture list from material
            for (int i = 0; i < renderList.Length; i++)
            {
                Material material = renderList[i].material;
                string piecePartName = renderList[i].transform.parent.name;
                if (piecePartName.EndsWith("_a")) piecePartName = piecePartName.Replace("_a", " (On)");
                if (piecePartName.EndsWith("_b")) piecePartName = piecePartName.Remove(piecePartName.Length - 2, 2) + " (Half)";
                string materialName = "<b>Material: </b>" + material.name.Replace("(Instance)", "").Trim() + "<b> Piece: </b>" + piecePartName;
                dropdownOptions.Add(materialName);
            }
            clothesDropdown.AddOptions(dropdownOptions);
            clothesDropdown.RefreshShownValue();
            clothesDropdown.onValueChanged.AddListener((UnityAction<int>)delegate
            {
                DrawTexturesGrid(renderList, dicMaterials, clothesDropdown);
            });
            lastDropdownSetting = -1;
            DrawTexturesGrid(renderList, dicMaterials, clothesDropdown);
        }

        private static int lastDropdownSetting = -1;
        private static Renderer[] renderList;
        public static void DrawTexturesGrid(Renderer[] renderList, Dictionary<int, MaterialContent> dicMaterials, Dropdown dropdown)
        {
            // Fixing Unity bug for duplicated calls
            if (dropdown.value == lastDropdownSetting) return;
            lastDropdownSetting = dropdown.value;

            Debug.Log("== MaterialID: " + dropdown.value.ToString());

            // Cleaning UI content
            for (int i = clothesTabContent.transform.childCount - 1; i >= 0; i--)
                GameObject.Destroy(clothesTabContent.transform.GetChild(i).gameObject);

            // Cleaning old miniatures
            for (int i = 0; i < miniatureTextures.Count; i++)
            {
                GarbageTextures.Add(miniatureTextures[i]);
                GarbageImages.Add(miniatureImages[i]);
            }
            miniatureTextures.Clear();

            int renderIndex = dropdown.value;
            {
                // Getting Texture list from material
                Material material = renderList[renderIndex].material;
                string materialName = material.name.Replace("(Instance)", "").Trim() + "-" + renderList[renderIndex].transform.parent.name;

                Debug.Log("== Material Name: " + materialName);

                Dictionary<string, Texture2D> materialTextures = GetMaterialTextures(material);

                if (!dicMaterials.ContainsKey(renderIndex)) dicMaterials.Add(renderIndex, new MaterialContent());
                MaterialContent materialContent = dicMaterials[renderIndex];
                if (materialContent.currentTextures == null) materialContent.currentTextures = new Dictionary<string, Texture2D>();
                Dictionary<string, Texture2D> storedTextues = materialContent.currentTextures;
                if (materialContent.originalTextures == null) materialContent.originalTextures = new Dictionary<string, Texture2D>();
                Dictionary<string, Texture2D> originalTextures = materialContent.currentTextures;

                // Creating one texture block for each texture
                for (int j = 0; j < materialTextures.Count; j++)
                {
                    string textureName = materialTextures.ElementAt(j).Key;
                    Texture2D materialTexture = materialTextures[textureName];

                    CreateTextureBlock(material, materialTexture, textureName, materialContent, clothesTabContent);
                }
            }
        }

        public static void CreateTextureBlock(Material material, Texture2D materialTexture, string textureName, MaterialContent materialContent, GameObject parent)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + textureName);
            textureGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Clothes Image
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * materialTexture.width / materialTexture.height;
            else height = width * materialTexture.height / materialTexture.width;

            Image miniature = RG_MaterialModUI.CreateImage(width, height);
            miniature.transform.SetParent(textureGroup.transform, false);
            UpdateMiniature(miniature, materialTexture, textureName);

            // Text with size
            string textContent = "Size: " + materialTexture.width.ToString() + "x" + materialTexture.height.ToString();
            Text text = RG_MaterialModUI.CreateText(textContent, 17, 200, 20);
            text.transform.SetParent(textureGroup.transform, false);

            // Clothes Set Button
            Button buttonSet = RG_MaterialModUI.CreateButton("Green  " + textureName, 16, 200, 35);
            buttonSet.onClick.AddListener((UnityAction)delegate { SetTextureButton(material, materialTexture, textureName, materialContent, miniature); });
            buttonSet.transform.SetParent(textureGroup.transform, false);

            // Clothes Reset Button
            Button buttonReset = RG_MaterialModUI.CreateButton("Reset " + textureName, 16, 200, 35);
            buttonReset.onClick.AddListener((UnityAction)delegate { ResetTextureButton(material, materialTexture, textureName, materialContent, miniature); });
            buttonReset.transform.SetParent(textureGroup.transform, false);

            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void UpdateMiniature(Image miniature, Texture2D texture, string textureName)
        {
            // maitaining proportions
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * texture.width / texture.height;
            else height = width * texture.height / texture.width;

            Texture2D scaledTexture = Resize(texture, width, height);

            // Trom pink maps to regular normal maps
            if (textureName.Contains("Bump"))
            {
                scaledTexture = DXT2nmToNormal(scaledTexture);
            }

            miniature.sprite = Sprite.Create(scaledTexture, new Rect(0, 0, width, height), new Vector2());
            miniatureTextures.Add(scaledTexture);
            miniatureImages.Add(miniature);
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void SetTextureButton(Material material, Texture2D materialTexture, string textureName, MaterialContent materialContent, Image miniature)
        {
            // In the future the load texture will be here
            Texture2D texture = new Texture2D(512, 512);
            texture = GreenTexture(512, 512);

            if (!materialContent.currentTextures.ContainsKey(textureName)) materialContent.currentTextures.Add(textureName, null);
            if (!materialContent.originalTextures.ContainsKey(textureName)) materialContent.originalTextures.Add(textureName, null);

            // Storing original texture
            if (materialContent.originalTextures[textureName] == null) materialContent.originalTextures[textureName] = materialTexture;

            // Reset old texture
            if (!(materialContent.currentTextures[textureName] == null)) GarbageTextures.Add(materialContent.currentTextures[textureName]);

            // Update Texture
            materialContent.currentTextures[textureName] = texture;
            material.SetTexture(textureName, materialContent.currentTextures[textureName]);

            // Update miniature
            UpdateMiniature(miniature, materialContent.currentTextures[textureName], textureName);

            DestroyGarbage();
        }

        public static void ResetTextureButton(Material material, Texture2D materialTexture, string textureName, MaterialContent materialContent, Image miniature)
        {
            if (!materialContent.currentTextures.ContainsKey(textureName)) return;

            material.SetTexture(textureName, materialContent.originalTextures[textureName]);
            UpdateMiniature(miniature, materialContent.originalTextures[textureName], textureName);

            // cleaning texture and entrances
            if (materialContent.currentTextures.ContainsKey(textureName)) GarbageTextures.Add(materialContent.currentTextures[textureName]);
            materialContent.currentTextures.Remove(textureName);

            DestroyGarbage();
        }

    }
}
