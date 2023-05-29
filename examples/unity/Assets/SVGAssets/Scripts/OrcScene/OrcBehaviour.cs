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
using System;
using UnityEngine;
using SVGAssets;

namespace WalkingOrcScene 
{

    public class OrcBehaviour : MonoBehaviour
    {
        private Vector3 CameraPosCalc(Vector3 orcPos)
        {
            // set the camera according to the orc position
            Vector3 cameraPos = new Vector3(orcPos.x, 0, -10);
            float cameraWorldLeft = cameraPos.x - (Camera.WorldWidth / 2);
            float cameraWorldRight = cameraPos.x + (Camera.WorldWidth / 2);

            // make sure the camera won't go outside the background
            if (cameraWorldLeft < Background.WorldLeft)
            {
                cameraPos.x += Background.WorldLeft - cameraWorldLeft;
            }
            if (cameraWorldRight > Background.WorldRight)
            {
                cameraPos.x -= cameraWorldRight - Background.WorldRight;
            }
            return cameraPos;
        }

        private void CameraPosAssign(Vector3 orcPos)
        {
            if ((Background != null) && (Camera != null))
            {
                // set the camera according to the orc position
                Camera.transform.position = (Camera.PixelWidth > Background.PixelWidth) ? new Vector3(0, 0, -10) : CameraPosCalc(orcPos);
            }
        }

        private float BackgroundWalkingLine()
        {
            // walking line is located at ~12% of the background half height, in world coordinates
            return (Background != null) ? (-Background.WorldHeight * 0.5f) * 0.12f : 0.0f;
        }

        private void ResetOrcPos()
        {
            Vector3 orcPos = new Vector3(0, BackgroundWalkingLine(), 0);
            // move the orc at the walking line
            transform.position = orcPos;
            CameraPosAssign(orcPos);
        }

        private void WalkAnimation()
        {
            if (m_Animator != null)
            {
                m_Animator.Play("walking");
            }
        }
    
        private void IdleAnimation()
        {
            if (m_Animator != null)
            {
                m_Animator.Play("idle");
            }
        }

        private void Move(Vector3 delta)
        {
            // move the orc
            Vector3 orcPos = transform.position + delta;
            // get the orc (body) sprite loader
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            float orcBodyWidth = spriteRenderer.sprite.bounds.size.x;
            // orc body pivot is located at 50% of the whole orc sprite, so we can calculate bounds easily
            float orcWorldLeft = orcPos.x - (orcBodyWidth / 2);
            float orcWorldRight = orcPos.x + (orcBodyWidth / 2);
            // make sure the orc won't go outside the background
            if (orcWorldLeft < Background.WorldLeft)
            {
                orcPos.x += (Background.WorldLeft - orcWorldLeft);
            }
            else
            if (orcWorldRight > Background.WorldRight)
            {
                orcPos.x -= (orcWorldRight - Background.WorldRight);
            }
            // update the orc position
            transform.position = orcPos;
            // set the camera according to the orc position
            CameraPosAssign(orcPos);
            WalkAnimation();
        }

        private void MoveLeft()
        {
            // flip the orc horizontally
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
            Move(new Vector3(-WALKING_SPEED, 0, 0));
        }
    
        private void MoveRight()
        {
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
            Move(new Vector3 (WALKING_SPEED, 0, 0));
        }

        private void ResizeBackground(int newScreenWidth, int newScreenHeight)
        {
            if (Background != null)
            {
                // we want to cover the whole screen
                Pair<SVGBackgroundScaleType, int> scaleData = Background.CoverFullScreen(newScreenWidth, newScreenHeight);
                Background.ScaleAdaption = scaleData.First;
                Background.Size = scaleData.Second;
                Background.UpdateBackground(false);
            }
        }

        private void ResizeOrcCharacter(int backgroundWidth, int backgroundHeight)
        {
            // get the orc (body) sprite loader
            SVGSpriteLoaderBehaviour spriteLoader = gameObject.GetComponent<SVGSpriteLoaderBehaviour>();
            // update/regenerate all orc sprites; NB: we want to size the orc according to
            // the background sprite (actually the background height)
            if (spriteLoader != null)
            {
                spriteLoader.UpdateSprite(true, backgroundWidth, backgroundHeight);
            }
        }

        private void OnResize(int newScreenWidth, int newScreenHeight)
        {
            // render the background at the right resolution
            ResizeBackground(newScreenWidth, newScreenHeight);
            // update/regenerate all orc sprites according to the background dimensions
            ResizeOrcCharacter((int)Background.PixelWidth, (int)Background.PixelHeight);
            // move the orc at the world origin and set the camera according to the orc position
            ResetOrcPos();
        }

        void Start()
        {
            // start with the "idle" animation
            m_Animator = gameObject.GetComponent<Animator>();
            IdleAnimation();
            // move the background at world origin and get the reference to its monobehaviour script
            if (Background != null)
            {
                Background.transform.position = new Vector3(0, 0, 0);
            }
            // register handler for device orientation change
            if (Camera != null)
            {
                // register ourself for receiving resize events
                Camera.OnResize += OnResize;
                // now fire a resize event by hand
                Camera.Resize(true);
            }
        }
    
        void LateUpdate()
        {
            if (Input.GetButton("Fire1"))
            {
                Vector3 worldMousePos = Camera.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            
                // get the orc (body) sprite loader
                SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                float orcBodyWidth = spriteRenderer.sprite.bounds.size.x;
                // orc body pivot is located at 50% of the whole orc sprite, so we can calculate bounds easily
                float orcWorldLeft = transform.position.x - (orcBodyWidth / 2);
                float orcWorldRight = transform.position.x + (orcBodyWidth / 2);

                if (worldMousePos.x > orcWorldRight)
                {
                    MoveRight();
                }
                else
                if (worldMousePos.x < orcWorldLeft)
                {
                    MoveLeft();
                }
            }
            else
            {
                IdleAnimation();
            }
        }

        // the scene camera
        public SVGCameraBehaviour Camera;
        // the background gameobject
        public SVGBackgroundBehaviour Background;
        // the orc animator
        private Animator m_Animator;
        // the walking speed, in world coordinates
        private const float WALKING_SPEED = 0.04f;
    }
}
