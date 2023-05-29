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
using System.Collections;
using UnityEngine;
using SVGAssets;

namespace MemoryGameScene
{
    public class GameBehaviour : MonoBehaviour
    {
        public Sprite UpdateCardSprite(GameCardBehaviour card)
        {
            if (Atlas != null)
            {
                GameCardType cardType = card.BackSide ? GameCardType.BackSide : card.AnimalType;
                SVGRuntimeSprite data = Atlas.GetSpriteByName(GameCardBehaviour.AnimalSpriteName(cardType));
                // get the sprite, given its name
                if (data != null)
                {
                    card.gameObject.GetComponent<SpriteRenderer>().sprite = data.Sprite;
                    // keep updated the SVGSpriteLoaderBehaviour component too
                    SVGSpriteLoaderBehaviour loader = card.gameObject.GetComponent<SVGSpriteLoaderBehaviour>();
                    if (loader != null)
                    {
                        loader.SpriteReference = data.SpriteReference;
                    }
                    return data.Sprite;
                }
            }
            return null;
        }

        private void UpdateCardsSprites()
        {
            // assign the new sprites and update colliders
            for (int i = 0; i < Cards.Length; ++i)
            {
                Sprite sprite = UpdateCardSprite(Cards[i]);
                if (sprite != null)
                {
                    Cards[i].GetComponent<BoxCollider2D>().size = sprite.bounds.size;
                }
            }
        }

        private void ResizeBackground(int newScreenWidth,
                                        int newScreenHeight)
        {
            if (Background != null)
            {
                // we want to cover the whole screen
                Background.SlicedWidth = newScreenWidth;
                Background.SlicedHeight = newScreenHeight;
                // render the background
                Background.UpdateBackground(true);
            }
        }

        private void ResizeCards(int newScreenWidth,
                                    int newScreenHeight)
        {
            if (Atlas != null)
            {
                // update card sprites according to the current screen resolution
                if (Atlas.UpdateRuntimeSprites(newScreenWidth, newScreenHeight, out float scale))
                {
                    // assign the new sprites and update colliders
                    UpdateCardsSprites();
                }
            }
        }

        private void PlaceCards(int screenWidth,
                                int screenHeight)
        {
            if (Camera != null)
            {
                int[] cardsIndexes;
                // number of card slots in each dimension
                int slotsPerRow, slotsPerColumn;
                SVGRuntimeSprite data = Atlas.GetSpriteByName(GameCardBehaviour.AnimalSpriteName(GameCardType.BackSide));
                float cardWidth = data.Sprite.bounds.size.x;
                float cardHeight = data.Sprite.bounds.size.y;
                float worldWidth = Camera.WorldWidth;
                float worldHeight = Camera.WorldHeight;
                bool invertCoord = false;

                // check actual orientation on iOS and Android devices
                if ((Application.platform == RuntimePlatform.Android) || (Application.platform == RuntimePlatform.IPhonePlayer))
                {
                    // portrait orientation
                    if (screenWidth <= screenHeight)
                    {
                        // number of card slots in each dimension
                        slotsPerRow = 3;
                        slotsPerColumn = 4;
                        cardsIndexes = s_CARDS_INDEXES_NATIVE_PORTRAIT;
                    }
                    else
                    {
                        // get current (landscape) orientation
                        ScreenOrientation orientation = SVGAssetsUnity.ScreenOrientation;

                        // number of card slots in each dimension
                        slotsPerRow = 4;
                        slotsPerColumn = 3;
                        invertCoord = true;
                        cardsIndexes = (orientation == ScreenOrientation.LandscapeRight) ? s_CARDS_INDEXES_LANDSCAPE_ROT90 : s_CARDS_INDEXES_LANDSCAPE_ROT270;
                    }
                }
                else
                {
                    // Desktop, detect orientation according to screen dimensions
                    slotsPerRow = (screenWidth <= screenHeight) ? 3 : 4;
                    slotsPerColumn = (screenWidth <= screenHeight) ? 4 : 3;
                    cardsIndexes = (screenWidth <= screenHeight) ? s_CARDS_INDEXES_NATIVE_PORTRAIT : s_CARDS_INDEXES_NATIVE_LANDSCAPE;
                }

                // 5% border
                float ofsX = worldWidth * 0.05f;
                float ofsY = worldHeight * 0.05f;
                float horizSeparator = ((worldWidth - (slotsPerRow * cardWidth) - (2.0f * ofsX)) / (slotsPerRow - 1));
                float vertSeparator = ((worldHeight - (slotsPerColumn * cardHeight) - (2.0f * ofsY)) / (slotsPerColumn - 1));
                int cardIdx = 0;

                for (int y = 0; y < slotsPerColumn; ++y)
                {
                    for (int x = 0; x < slotsPerRow; ++x)
                    {
                        float posX = ofsX + (x * (cardWidth + horizSeparator)) - (worldWidth * 0.5f) + (cardWidth * 0.5f);
                        float posY = ofsY + (y * (cardHeight + vertSeparator)) - (worldHeight * 0.5f) + (cardHeight * 0.5f);
                        // invert coordinates, if needed
                        Cards[cardsIndexes[cardIdx]].transform.position = invertCoord ? new Vector3(-posX, -posY) : new Vector3(posX, posY);
                        cardIdx++;
                    }
                }
            }
        }

        private void OnResize(int newScreenWidth,
                                int newScreenHeight)
        {
            // resize the splash screen
            SplashScreen.Resize(newScreenWidth, newScreenHeight);
            // resize the background
            ResizeBackground(newScreenWidth, newScreenHeight);
            // resize animals sprites
            ResizeCards(newScreenWidth, newScreenHeight);
            // resize congratulation message
            CongratsMessage.Resize(newScreenWidth, newScreenHeight);
            // rearrange cards on the screen
            PlaceCards(newScreenWidth, newScreenHeight);
        }

        public void HideCard(GameCardBehaviour card)
        {
            card.Active = false;
            // disable renderer
            card.gameObject.GetComponent<SpriteRenderer>().enabled = false;
            // disable collider
            card.GetComponent<BoxCollider2D>().enabled = false;
        }
    
        public void ShowCard(GameCardBehaviour card)
        {
            card.Active = true;
            // enable renderer
            card.gameObject.GetComponent<SpriteRenderer>().enabled = true;
            // enable collider
            card.GetComponent<BoxCollider2D>().enabled = true;
        }

        private void TurnCard(GameCardBehaviour card, bool backSide)
        {
            card.BackSide = backSide;
            UpdateCardSprite(card);
        }

        private void Shuffle(GameCardType[] array)
        {
            System.Random rnd = new System.Random(Environment.TickCount);
            int n = array.Length;

            // Knuth shuffle
            while (n > 1)
            {
                n--;
                int i = rnd.Next(n + 1);
                GameCardType temp = array[i];
                array[i] = array[n];
                array[n] = temp;
            }
        }

        private IEnumerator ShuffleAnimation()
        {
            _animating = true;
            for (int i = 0; i < Cards.Length; ++i)
            {
                Cards[i].GetComponent<Animation>().PlayQueued("cardRotation");
            }
            yield return new WaitForSeconds(2);
            _animating = false;
        }

        private IEnumerator ShowSplashScreen()
        {
            // hide background
            Background.gameObject.SetActive(false);
            // hide cards
            foreach (GameCardBehaviour card in Cards)
            {
                card.gameObject.SetActive(false);
            }
            // show splash screen
            SplashScreen.gameObject.SetActive(true);
            // wait...
            yield return new WaitForSeconds(3);
            // hide splash screen
            SplashScreen.gameObject.SetActive(false);
            // show background
            Background.gameObject.SetActive(true);
            // show cards
            foreach (GameCardBehaviour card in Cards)
            {
                card.gameObject.SetActive(true);
            }
            // enable "shuffle" animation on cards
            StartCoroutine(ShuffleAnimation());
        }

        private void StartNewGame()
        {
            GameCardType[] animalCouples = new GameCardType[Cards.Length];
            // start with a random animal
            GameCardType currentAnimal = GameCardBehaviour.RandomAnimal();

            // generate animal couples
            for (int i = 0; i < (Cards.Length / 2); ++i)
            {
                animalCouples[i * 2] = currentAnimal;
                animalCouples[(i * 2) + 1] = currentAnimal;
                currentAnimal = GameCardBehaviour.NextAnimal(currentAnimal);
            }
            // shuffle couples
            Shuffle(animalCouples);
            // assign cards
            for (int i = 0; i < Cards.Length; ++i)
            {
                Cards[i].BackSide = true;
                Cards[i].AnimalType = animalCouples[i];
                ShowCard(Cards[i]);
            }

            // select a background
            if (Background != null)
            {
                if (BackgroundFiles.Length > 0)
                {
                    // destroy current texture and sprite
                    Background.DestroyAll(true);
                    // assign a new SVG file
                    Background.SVGFile = BackgroundFiles[_backgroundIndex % BackgroundFiles.Length];
                    _backgroundIndex++;
                }
                else
                {
                    Background.SVGFile = null;
                }
            }

            // no selection
            _selectedCard0 = null;
            _selectedCard1 = null;
        }

        // Is the current game stage completed?
        private bool StageCompleted()
        {
            bool completed = true;

            foreach (GameCardBehaviour card in Cards)
            {
                if (card.Active)
                {
                    completed = false;
                    break;
                }
            }

            // stage is completed if all cards are inactive
            return completed;
        }

        private IEnumerator WrongCouple()
        {
            yield return new WaitForSeconds(1.5f);
            // show card back face
            TurnCard(_selectedCard0, true);
            TurnCard(_selectedCard1, true);
            _selectedCard0 = _selectedCard1 = null;
        }

        private IEnumerator GoodCouple()
        {
            _animating = true;
            _selectedCard0.GetComponent<Animation>().PlayQueued("cardRotation");
            _selectedCard1.GetComponent<Animation>().PlayQueued("cardRotation");
            yield return new WaitForSeconds(2);
            _animating = false;
            // hide both cards
            HideCard(_selectedCard0);
            HideCard(_selectedCard1);
            _selectedCard0 = _selectedCard1 = null;
            // if we have finished, start a new game!
            if (StageCompleted())
            {
                // show congratulation message
                CongratsMessage.gameObject.SetActive(true);
                // wait...
                yield return new WaitForSeconds(3);
                // hide congratulation message
                CongratsMessage.gameObject.SetActive(false);

                // start a new game: shuffle the cards deck and change the background
                StartNewGame();
                // assign the new sprites and update colliders
                UpdateCardsSprites();
                // because we have changed SVG file, we want to be sure that the new background will cover the whole screen
                ResizeBackground((int)SVGAssetsUnity.ScreenWidth, (int)SVGAssetsUnity.ScreenHeight);
                // show the "shuffle" animation
                StartCoroutine(ShuffleAnimation());
            }
        }

        public void SelectCard(GameCardBehaviour card)
        {
            // avoid selection during animation
            if (!_animating)
            {
                // card is already in the current selection
                if ((card != _selectedCard0) && (card != _selectedCard1))
                {
                    // select the first card
                    if (_selectedCard0 == null)
                    {
                        _selectedCard0 = card;
                        // show card front face
                        TurnCard(card, false);
                    }
                    else
                    // select the second card
                    if (_selectedCard1 == null)
                    {
                        _selectedCard1 = card;
                        // show card front face
                        TurnCard(card, false);
                        // if the couple does not match simply turn cards backside, else animate and hide them
                        StartCoroutine(_selectedCard0.AnimalType == _selectedCard1.AnimalType ? "GoodCouple" : "WrongCouple");
                    }
                }
            }
        }

        // Use this for initialization
        void Start()
        {
            _animating = false;
            // select a random background
            _backgroundIndex = Environment.TickCount % BackgroundFiles.Length;
            // start a new game
            StartNewGame();

            if (Camera != null)
            {
                // register ourself for receiving resize events
                Camera.OnResize += OnResize;
                // now fire a resize event by hand
                Camera.Resize(true);
            }
            // show "Powered by AmanithSVG" splash screen, then shuffle cards
            StartCoroutine(ShowSplashScreen());
        }

    #if UNITY_EDITOR
        // Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        // This function is only called in editor mode. Reset is most commonly used to give good default values in the inspector.
        void Reset()
        {
            Camera = null;
            SplashScreen = null;
            Background = null;
            Atlas = null;
            CongratsMessage = null;
            Cards = null;
            _selectedCard0 = null;
            _selectedCard1 = null;
            _animating = false;
        }
    #endif
        // the main camera, used to intercept screen resize events
        public SVGCameraBehaviour Camera;
        // "Powered by AmanithSVG" splash screen
        public GameSplashScreenBehaviour SplashScreen;
        // the game background
        public SVGBackgroundBehaviour Background;
        // the atlas used to generate animals sprite
        public SVGAtlas Atlas;
        // Congratulation message "you win!"
        public GameCongratsBehaviour CongratsMessage;
        // the deck of cards
        public GameCardBehaviour[] Cards;
        // array of usable SVG backgrounds
        public TextAsset[] BackgroundFiles;
        // the first selected card (could be null)
        [NonSerialized]
        private GameCardBehaviour _selectedCard0;
        // the second selected card (could be null)
        [NonSerialized]
        private GameCardBehaviour _selectedCard1;
        // true if some card is playing an animation, else false
        [NonSerialized]
        private bool _animating;
        // the current background (i.e. the index within the BackgroundFiles array)
        [NonSerialized]
        private int _backgroundIndex;
        // cards arrangement when native device orientation is portrait
        private static readonly int[] s_CARDS_INDEXES_NATIVE_PORTRAIT = {
            0,  1,  2,
            3,  4,  5,
            6,  7,  8,
            9, 10, 11
        };
        // landscape orientation, clockwise from the portrait orientation
        private static readonly int[] s_CARDS_INDEXES_LANDSCAPE_ROT90 = {
            9, 6, 3, 0,
            10, 7, 4, 1,
            11, 8, 5, 2
        };
        // landscape orientation, counter-clockwise from the portrait orientation
        private static readonly int[] s_CARDS_INDEXES_LANDSCAPE_ROT270 = {
            2, 5, 8, 11,
            1, 4, 7, 10,
            0, 3, 6,  9
        };

        // cards arrangement when native device orientation is landscape
        private static readonly int[] s_CARDS_INDEXES_NATIVE_LANDSCAPE = {
            0,  1,  2, 3,
            4,  5,  6, 7,
            8,  9, 10, 11
        };
    }
}
