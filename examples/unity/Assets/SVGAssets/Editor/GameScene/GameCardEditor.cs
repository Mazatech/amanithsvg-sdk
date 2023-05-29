/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
** 
** Redistribution and use in source and binary forms, with or without
** modification, are permitted (subject to the limitations in the disclaimer
** below) provided that the following conditions are met:
** 
** - Redistributions of source code must retain the above copyright notice,
**   this list of conditions and the following disclaimer.
** 
** - Redistributions in binary form must reproduce the above copyright notice,
**   this list of conditions and the following disclaimer in the documentation
**   and/or other materials provided with the distribution.
** 
** - Neither the name of Mazatech S.r.l. nor the names of its contributors
**   may be used to endorse or promote products derived from this software
**   without specific prior written permission.
** 
** NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED
** BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
** CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT
** NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
** A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER
** OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
** EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
** PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
** OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
** WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
** OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
** ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
** 
** For any information, please contact info@mazatech.com
** 
****************************************************************************/
using UnityEngine;
using UnityEditor;
using SVGAssets;

namespace MemoryGameScene {

    [ CustomEditor(typeof(GameCardBehaviour)) ]
    public class GameCardEditor : Editor
    {
        private void DrawInspector(GameCardBehaviour card)
        {
            bool needSpriteUpdate = false;
            bool active = EditorGUILayout.Toggle(new GUIContent("Active", ""), card.Active);
            bool backSide = EditorGUILayout.Toggle(new GUIContent("Back side", ""), card.BackSide);
            GameCardType animalType = (GameCardType)EditorGUILayout.EnumPopup("Animal type", card.AnimalType);
            GameBehaviour game = EditorGUILayout.ObjectField("Game manager", card.Game, typeof(GameBehaviour), true) as GameBehaviour;

            // update active flag, if needed
            if (active != card.Active)
            {
                card.Active = active;
                SVGUtils.MarkSceneDirty();
                if (card.Game != null)
                {
                    if (card.Active)
                    {
                        card.Game.ShowCard(card);
                    }
                    else
                    {
                        card.Game.HideCard(card);
                    }
                }
            }
            // update back side flag, if needed
            if (backSide != card.BackSide)
            {
                card.BackSide = backSide;
                SVGUtils.MarkSceneDirty();
                needSpriteUpdate = true;
            }
            // update animal/card type, if needed
            if (animalType != card.AnimalType)
            {
                card.AnimalType = animalType;
                SVGUtils.MarkSceneDirty();
                needSpriteUpdate = true;
            }
            // update game manager, if needed
            if (game != card.Game)
            {
                card.Game = game;
                SVGUtils.MarkSceneDirty();
            }

            if (needSpriteUpdate && (card.Game != null))
            {
                card.Game.UpdateCardSprite(card);
            }
        }

        public override void OnInspectorGUI()
        {
            // get the target object
            GameCardBehaviour card = target as GameCardBehaviour;
        
            if (card != null)
            {
                DrawInspector(card);
            }
        }
    }
}
