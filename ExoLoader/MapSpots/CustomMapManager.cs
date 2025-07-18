﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExoLoader
{
    internal class CustomMapManager
    {
        public static Dictionary<string, GameObject> mapObjects = new Dictionary<string, GameObject>();
        private static MapObjectFactory objectFactory = new MapObjectFactory();

        public static void MakeCustomMapObject(CustomChara chara, string season, int week, string scene)
        {
            try
            {
                if (!IsValidScene(chara, scene) || chara.isDead)
                {
                    RemoveMapObject(chara.charaID);
                    ModInstance.log("Tried making " + chara.charaID + " model in unsuitable scene " + scene);
                    return;
                }

                if (mapObjects.ContainsKey(chara.charaID))
                {
                    HandleExistingMapObject(chara, scene);
                    return;
                }

                CreateNewMapObject(chara, season, week, scene);
            }
            catch (Exception e)
            {
                ModInstance.log("Error while trying to get map object for " + chara.charaID + ": " + e.Message);
                ModInstance.log(e.ToString());
            }
        }

        private static bool IsValidScene(CustomChara chara, string scene)
        {
            return ((scene.Equals("strato") || scene.Equals("stratodestroyed")) && !chara.data.helioOnly) || scene.Equals("helio");
        }

        private static void HandleExistingMapObject(CustomChara chara, string scene)
        {
            GameObject existingObject = mapObjects[chara.charaID];

            if (existingObject != null)
            {
                if (ShouldRemoveFromScene(chara, scene))
                {
                    RemoveMapObject(chara.charaID);
                    return;
                }

                if (ShouldMoveMapObject(existingObject, chara, scene))
                {
                    MoveMapObject(existingObject, chara, scene);
                }
            }
            else
            {
                CreateNewMapObject(chara, Princess.season.seasonID, Princess.monthOfSeason, scene);
            }
        }

        private static void CreateNewMapObject(CustomChara chara, string season, int week, string scene)
        {
            GameObject templateObject = objectFactory.GetMapObjectTemplate(chara.data.skeleton, season, week);

            if (templateObject == null)
            {
                ModInstance.log("Couldn't get base map object, cancelling map spot creation for : " + chara.charaID);
                return;
            }

            GameObject customMapObject = objectFactory.CreateCustomMapObject(templateObject, chara, scene);

            if (customMapObject != null)
            {
                mapObjects[chara.charaID] = customMapObject;
            }
        }

        private static bool ShouldMoveMapObject(GameObject mapObject, CustomChara chara, string scene)
        {
            float[] targetPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (targetPosition == null)
            {
                ModInstance.log($"Character {chara.charaID} should not be on scene {scene}");
                return false;
            }

            Transform currentTransform = mapObject.GetComponentInChildren<Transform>();

            if (currentTransform == null)
            {
                ModInstance.log($"Map object for {chara.charaID} has no Transform component.");
                return true;
            }

            // y coordinate is weird, so we only check x and z
            Vector3 currentPosition = new Vector3(currentTransform.localPosition.x, 0, currentTransform.localPosition.z);
            Vector3 targetPos = new Vector3(targetPosition[0], 0, targetPosition[2]);

            float tolerance = 0.01f;
            return Vector3.Distance(currentPosition, targetPos) > tolerance;
        }

        private static bool ShouldRemoveFromScene(CustomChara chara, string scene)
        {
            float[] position = chara.GetMapSpot(scene, Princess.season.seasonID);
            return position == null;
        }

        private static void MoveMapObject(GameObject mapObject, CustomChara chara, string scene)
        {
            float[] newPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (newPosition == null)
            {
                ModInstance.log($"Cannot move {chara.charaID} - no valid position for scene {scene}");
                return;
            }

            mapObject.transform.localPosition = new Vector3(newPosition[0], newPosition[1], newPosition[2]);

            MapSpot mapSpot = mapObject.GetComponent<MapSpot>();
            if (mapSpot != null)
            {
                mapSpot.MoveToGround();
            }

            ModInstance.log($"Moved {chara.charaID} to position ({newPosition[0]}, {newPosition[1]}, {newPosition[2]})");
        }

        private static void RemoveMapObject(string charaID)
        {
            if (mapObjects.ContainsKey(charaID))
            {
                GameObject obj = mapObjects[charaID];
                mapObjects.Remove(charaID);

                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }

                ModInstance.log($"Removed map object for {charaID}");
            }
        }
    }
}