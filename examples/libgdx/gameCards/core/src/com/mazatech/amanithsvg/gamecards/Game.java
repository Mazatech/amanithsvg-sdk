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
package com.mazatech.amanithsvg.gamecards;

import java.util.HashMap;
import java.util.Random;

// libGDX
import com.badlogic.gdx.Application;
import com.badlogic.gdx.ApplicationAdapter;
import com.badlogic.gdx.Gdx;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;
import com.badlogic.gdx.graphics.glutils.HdpiUtils;
import com.badlogic.gdx.graphics.OrthographicCamera;
import com.badlogic.gdx.Input;
import com.badlogic.gdx.InputProcessor;
import com.badlogic.gdx.utils.ScreenUtils;
import com.badlogic.gdx.utils.Timer;

// AmanithSVG for libGDX
import com.mazatech.gdx.SVGAssetsGDX;
import com.mazatech.gdx.SVGAssetsConfigGDX;
import com.mazatech.gdx.SVGTexture;
import com.mazatech.gdx.SVGTextureAtlas;
import com.mazatech.gdx.SVGTextureAtlasGenerator;
import com.mazatech.gdx.SVGTextureAtlasPage;
import com.mazatech.gdx.SVGTextureAtlasRegion;

// AmanithSVG java binding (high level layer)
import com.mazatech.svgt.SVGAssets;
import com.mazatech.svgt.SVGDocument;
import com.mazatech.svgt.SVGScaler;
import com.mazatech.svgt.SVGScalerMatchMode;
// AmanithSVG java binding (low level layer)
import com.mazatech.svgt.SVGTAlign;
import com.mazatech.svgt.SVGTError;
import com.mazatech.svgt.SVGTMeetOrSlice;

public class Game extends ApplicationAdapter implements InputProcessor {

    // display some basic information about installed AmanithSVG library
    private void amanithsvgInfoDisplay() {

        // vendor
        Gdx.app.log(s_LOG_TAG, "AmanithSVG vendor = " + SVGAssets.getVendor());
        // version
        Gdx.app.log(s_LOG_TAG, "AmanithSVG version = " + SVGAssets.getVersion());
    }

    private void generateAnimalSprites(int screenWidth,
                                       int screenHeight) {

        // the scaler will calculate the correct scaling factor, actual parameters say:
        // "We have created all the SVG files (that we are going to pack in atlas) so
        // that, at 768 x 640 (the 'reference resolution'), they do not need additional
        // scaling (the last passed parameter value 1 is the basic scale relative to
        // the 'reference resolution'); if the device has a different screen resolution,
        // we want to scale SVG contents depending on the actual width and height
        // (MatchWidthOrHeight), equally important (0.5f)"
        SVGScaler scaler = new SVGScaler(768, 640, SVGScalerMatchMode.MatchWidthOrHeight, 0.5f, 1);
        // calculate the scale factor according to the current window/screen resolution
        float scale = scaler.scaleFactorCalc(screenWidth, screenHeight);

        // re-generate sprites just if needed (e.g. we don't want to generate them if device has been rotated)
        if (scale != _spritesGenerationScale) {

            // dispose previous textures atlas
            if (_atlas != null) {
                _atlas.dispose();
                _atlas = null;
            }

            // set generation scale (all other parameters have been set when instantiating the SVGTextureAtlasGenerator class)
            _atlasGen.setScale(scale);

            // do the real generation
            try {
                _svg.logInfo("Loading and parsing " + s_CARDS_SVG);
                _atlas = _atlasGen.generateAtlas();
            }
            catch (SVGTextureAtlasGenerator.SVGTextureAtlasPackingException e) {
                Gdx.app.log(s_LOG_TAG, "Some SVG elements cannot be packed!");
                Gdx.app.log(s_LOG_TAG, "Specified maximum texture dimensions (in conjunction with specified scale factor), do not allow the packing of all SVG elements");
            }

            // empty the previous map (i.e. the association of animal types the respective texture region)
            _animalsSprites.clear();

            // now associate to each animal type the respective texture region
            for (SVGTextureAtlasPage page : _atlas.getPages()) {
                for (SVGTextureAtlasRegion region : page.getRegions()) {
                    _animalsSprites.put(CardType.fromName(region.getElemName()), region);
                }
            }
            // keep track of the scale factor at which animal sprites have been generated
            _spritesGenerationScale = scale;
        }
    }

    // draw the given texture by centering it on the screen
    private void drawCenteredTexture(SVGTexture texture,
                                     int screenWidth,
                                     int screenHeight) {

        int texWidth = texture.getWidth();
        int texHeight = texture.getHeight();
        int x = (screenWidth - texWidth) / 2;
        int y = (screenHeight - texHeight) / 2;

        // draw texture at the center of screen
        _batch.draw(texture,
                    // destination screen region
                    x, y, texWidth, texHeight,
                    // source texture region
                    0, 0, texWidth, texHeight,
                    // flipX, flipY
                    false, true);
    }

    // generate "Powered by AmanithSVG" splash screen texture
    private void generateSplashScreen(int screenWidth,
                                      int screenHeight) {

        // get 'viewBox' dimensions
        float viewW = _splashDoc.getViewport().getWidth();
        float viewH = _splashDoc.getViewport().getHeight();
        // keep the SVG aspect ratio
        float sx = screenWidth / viewW;
        float sy = screenHeight / viewH;
        // keep 2% border on each side
        float scaleMin = Math.min(sx, sy) * 0.96f;
        // and at the same time we fit the screen
        int texW = Math.round(viewW * scaleMin);
        int texH = Math.round(viewH * scaleMin);

        // destroy previous texture, if needed
        if (_splashTexture != null) {
            _splashTexture.dispose();
        }

        _splashTexture = _svg.createTexture(_splashDoc, texW, texH);
    }

    // generate the background texture
    private void generateBackground(int screenWidth,
                                    int screenHeight) {

        // destroy previous texture, if needed
        if (_backgroundTexture != null) {
            _backgroundTexture.dispose();
        }

        _backgroundTexture = _svg.createTexture(_backgroundDocs[_backgroundIdx], screenWidth, screenHeight);
    }

    private void generateCongratsMessage(int screenWidth,
                                         int screenHeight) {

        // congratulation SVG is squared by design, we choose to generate a texture with
        // a size equal to 3/5 of the smallest screen dimension; e.g. on a 1920 x 1080
        // device screen, texture will have a size of (1080 * 3) / 5 = 648 pixels
        int size = (Math.min(screenWidth, screenHeight) * 3) / 5;

        // destroy previous texture, if needed
        if (_congratsTexture != null) {
            _congratsTexture.dispose();
        }

        _congratsTexture = _svg.createTexture(_congratsDoc, size, size);
    }

    // position cards on the screen
    private void placeCards(int screenWidth,
                            int screenHeight) {

        int[] cardsIndexes;
        Application.ApplicationType appType = Gdx.app.getType();
        SVGTextureAtlasRegion region = _animalsSprites.get(CardType.BackSide);
        int cardWidth = region.getRegionWidth();
        int cardHeight = region.getRegionWidth();
        // number of card slots in each dimension
        int slotsPerRow = (screenWidth <= screenHeight) ? 3 : 4;
        int slotsPerColumn = (screenWidth <= screenHeight) ? 4 : 3;
        // 5% border
        int borderX = (int)Math.floor(screenWidth * 0.05);
        int borderY = (int)Math.floor(screenHeight * 0.05);
        // space between one card and the adjacent one
        int horizSeparator = ((screenWidth - (slotsPerRow * cardWidth) - (2 * borderX)) / (slotsPerRow - 1));
        int vertSeparator = ((screenHeight - (slotsPerColumn * cardHeight) - (2 * borderY)) / (slotsPerColumn - 1));
        int i = 0;

        // check actual orientation on iOS and Android devices
        if ((appType == Application.ApplicationType.Android) ||
            (appType == Application.ApplicationType.iOS)) {

            // get current orientation
            int deviceRotation = Gdx.input.getRotation();

            if (_nativeDeviceOrientation == Input.Orientation.Portrait) {
                // native orientation is portrait, now handle rotations
                switch (deviceRotation) {
                    case 90:
                        cardsIndexes = s_CARDS_INDEXES_LANDSCAPE_ROT90;
                        break;
                    case 270:
                        cardsIndexes = s_CARDS_INDEXES_LANDSCAPE_ROT270;
                        break;
                    default:
                        cardsIndexes = s_CARDS_INDEXES_NATIVE_PORTRAIT;
                        break;
                }
            }
            else {
                // native orientation is landscape, now handle rotations
                switch (deviceRotation) {
                    case 90:
                        cardsIndexes = s_CARDS_INDEXES_PORTRAIT_ROT90;
                        break;
                    case 270:
                        cardsIndexes = s_CARDS_INDEXES_PORTRAIT_ROT270;
                        break;
                    default:
                        cardsIndexes = s_CARDS_INDEXES_NATIVE_LANDSCAPE;
                        break;
                }
            }
        }
        else {
            // Desktop, HeadlessDesktop, Applet; detect orientation according to screen dimensions
            cardsIndexes = (screenWidth <= screenHeight) ? s_CARDS_INDEXES_NATIVE_PORTRAIT : s_CARDS_INDEXES_NATIVE_LANDSCAPE;
        }

        for (int y = 0; y < slotsPerColumn; ++y) {
            for (int x = 0; x < slotsPerRow; ++x) {
                region = _animalsSprites.get(_cards[cardsIndexes[i]].animalType);
                _cards[cardsIndexes[i]].x = borderX + (x * (cardWidth + horizSeparator));
                _cards[cardsIndexes[i]].y = borderY + (y * (cardHeight + vertSeparator));
                _cards[cardsIndexes[i]].width = region.getRegionWidth();
                _cards[cardsIndexes[i]].height = region.getRegionHeight();
                i++;
            }
        }
    }

    private void selectCard(int screenX,
                            int screenY) {

        for (Card card : _cards) {
            if (card.active) {
                // check if the card has been touched
                if ((screenX > card.x) && (screenX < (card.x + card.width)) && (screenY > card.y) && (screenY < (card.y + card.height))) {
                    // card is already in the current selection
                    if ((card != _selectedCard0) && (card != _selectedCard1)) {
                        // select the first card
                        if (_selectedCard0 == null) {
                            _selectedCard0 = card;
                            // show card front face
                            _selectedCard0.backSide = false;
                        }
                        else
                        // select the second card
                        if (_selectedCard1 == null) {
                            // show card front face
                            _selectedCard1 = card;
                            _selectedCard1.backSide = false;
                            // wait some seconds
                            _inputDisabled = true;
                            Timer.schedule(new Timer.Task() {
                                @Override public void run() {
                                    // the couple matches, hide cards
                                    if (_selectedCard0.animalType == _selectedCard1.animalType) {
                                        _selectedCard0.active = _selectedCard1.active = false;
                                    }
                                    else {
                                        // the couple does not match, turn cards backside again
                                        _selectedCard0.backSide = _selectedCard1.backSide = true;
                                    }
                                    // deselect cards
                                    _selectedCard0 = _selectedCard1 = null;
                                    // stop waiting
                                    _inputDisabled = false;
                                }
                            }, 1.5f);
                        }
                    }
                    break;
                }
            }
        }
    }

    // is the current game stage completed?
    private boolean stageCompleted() {

        boolean completed = true;

        for (Card card : _cards) {
            // if there is at least one active card left, the game is not over
            if (card.active) {
                completed = false;
                break;
            }
        }
        // stage is completed if all cards are inactive
        return completed;
    }

    // start a new game: shuffle the cards deck and change the background
    private void startNewGame() {

        CardType[] animalCouples = new CardType[s_CARDS_COUNT];
        // start with a random animal
        CardType currentAnimal = CardType.random();

        // generate animal couples
        for (int i = 0; i < (s_CARDS_COUNT / 2); ++i) {
            animalCouples[i * 2] = currentAnimal;
            animalCouples[(i * 2) + 1] = currentAnimal;
            currentAnimal = currentAnimal.next();
        }

        // shuffle couples
        Random rnd = new Random();
        int n = s_CARDS_COUNT;
        // Knuth shuffle
        while (n > 1) {
            n--;
            int i = rnd.nextInt(n + 1);
            CardType temp = animalCouples[i];
            animalCouples[i] = animalCouples[n];
            animalCouples[n] = temp;
        }

        // assign cards
        for (int i = 0; i < s_CARDS_COUNT; ++i) {
            // cards start as active and backside
            _cards[i] = new Card(animalCouples[i]);
        }

        // select a new background
        _backgroundIdx = (_backgroundIdx + 1) % 4;

        // no selection
        _selectedCard0 = null;
        _selectedCard1 = null;
        // enable player input
        _inputDisabled = false;
    }

    // display "Powered by AmanithSVG" splash screen
    private void drawSplashScreen(int screenWidth,
                                  int screenHeight) {

        // clear screen
        ScreenUtils.clear(0.439f, 0.502f, 0.565f, 1);
        // draw splash screen texture
        _batch.begin();
        drawCenteredTexture(_splashTexture, screenWidth, screenHeight);
        _batch.end();
    }

    // draw the game, background and cards
    private void drawGame(int screenWidth,
                          int screenHeight) {

        // clear screen
        ScreenUtils.clear(1, 1, 1, 1);

        // start drawing
        _batch.begin();
        // draw the background (texture, x, y, width, height, srcX, srcY, srcWidth, srcHeight, flipX, flipY)
        _batch.draw(_backgroundTexture, 0, 0, screenWidth, screenHeight, 0, 0, _backgroundTexture.getWidth(), _backgroundTexture.getHeight(), false, true);
        // draw cards
        for (Card card : _cards) {
            // draw active cards only
            if (card.active) {
                SVGTextureAtlasRegion region = _animalsSprites.get(card.backSide ? CardType.BackSide : card.animalType);
                if (region != null) {
                    _batch.draw(region, card.x, card.y);
                }
            }
        }
        // finish drawing
        _batch.end();
    }

    // draw the background and the congratulation message
    private void drawCongratsMessage(int screenWidth,
                                     int screenHeight) {

        // clear screen
        ScreenUtils.clear(1, 1, 1, 1);
        // start drawing
        _batch.begin();
        // draw the background (texture, x, y, width, height, srcX, srcY, srcWidth, srcHeight, flipX, flipY)
        _batch.draw(_backgroundTexture, 0, 0, screenWidth, screenHeight, 0, 0, _backgroundTexture.getWidth(), _backgroundTexture.getHeight(), false, true);
        // draw congratulation message at the center of screen
        drawCenteredTexture(_congratsTexture, screenWidth, screenHeight);
        // finish drawing
        _batch.end();
    }

    // clear AmanithSVG log buffer
    private void logClear() {

        // clear AmanithSVG log buffer
        if (_svg.logClear(false) != SVGTError.None) {
            Gdx.app.log(s_LOG_TAG, "Error clearing AmanithSVG log buffer\n");
        }
    }

    // print AmanithSVG log buffer content
    private void logOutput(String headerMessage,
                           boolean stopLogging) {

        // get AmanithSVG log content as a string
        String str = _svg.getLog();

        // avoid to print empty log
        if (!str.isEmpty()) {
            Gdx.app.log(s_LOG_TAG, headerMessage);
            Gdx.app.log(s_LOG_TAG, str);
        }

        if (stopLogging) {
            // make sure AmanithSVG no longer uses a log buffer (i.e. disable logging)
            if (_svg.logClear(true) != SVGTError.None) {
                Gdx.app.log(s_LOG_TAG, "Error stopping AmanithSVG logging\n");
            }
        }
    }

    @Override
    public void create() {

        // NB: use the backbuffer to get the real dimensions in pixels
        int screenWidth = Gdx.graphics.getBackBufferWidth();
        int screenHeight = Gdx.graphics.getBackBufferHeight();
        int deviceRotation = Gdx.input.getRotation();
        // create configuration for AmanithSVG
        SVGAssetsConfigGDX cfg = new SVGAssetsConfigGDX(screenWidth, screenHeight, Gdx.graphics.getPpiX());

        // set curves quality (used by AmanithSVG geometric kernel to approximate curves with straight
        // line segments (flattening); valid range is [1; 100], where 100 represents the best quality
        cfg.setCurvesQuality(75);

        // specify the system/user-agent language; this setting will affect the conditional rendering
        // of <switch> elements and elements with 'systemLanguage' attribute specified
        cfg.setLanguage("en");

        // provide fonts, in order to render <text> elements
        cfg.addFont("acme.ttf", "Acme");

        // detect native device orientation (because libGDX getNativeOrientation method is not
        // always correct (for example, on iOS it returns the current device orientation, not
        // the native one, see https://git.io/JEFzU )
        if (((deviceRotation == 0 || deviceRotation == 180) && (screenWidth >= screenHeight)) ||
            ((deviceRotation == 90 || deviceRotation == 270) && (screenWidth <= screenHeight))) {
            _nativeDeviceOrientation = Input.Orientation.Landscape;
        }
        else {
            _nativeDeviceOrientation = Input.Orientation.Portrait;
        }

        // initialize AmanithSVG for libGDX
        _svg = new SVGAssetsGDX(cfg);

        // load splash screen SVG document
        _svg.logInfo("Loading and parsing " + s_SPLASH_SVG);
        _splashDoc = _svg.createDocumentFromFile(s_SPLASH_SVG);

        // load backgrounds SVG documents
        for (int i = 0; i < 4; ++i) {
            _svg.logInfo("Loading and parsing gameBkg" + (i + 1) + ".svg");
            _backgroundDocs[i] = _svg.createDocumentFromFile("gameBkg" + (i + 1) + ".svg");
            // backgrounds viewBox must cover the whole drawing surface, so we use
            // SVGTMeetOrSlice.Slice (default is SVGTMeetOrSlice.Meet)
            _backgroundDocs[i].setAspectRatio(SVGTAlign.XMidYMid, SVGTMeetOrSlice.Slice);
        }
        // select a random background
        _backgroundIdx = (int)(Math.random() * 3);

        // load congratulations SVG document
        _svg.logInfo("Loading and parsing " + s_CONGRATS_SVG);
        _congratsDoc = _svg.createDocumentFromFile(s_CONGRATS_SVG);

        // scale, border, dilateEdgesFix
        _atlasGen = _svg.createAtlasGenerator(1, 1, false);
        // SVG file, explodeGroups, scale
        // NB: because 'gameAnimals.svg' has been designed for a 768 x 640 resolution (see
        // the file header), we do not want to adjust the scale further (i.e. we pass
        // 1 as additional scale factor)
        // Note that at the 768 x 640 'reference resolution', each animal sprite will
        // have a dimension of 128 x 128: it will ALWAYS guarantee that we can easily
        // place them on a 4 x 3 grid (landscape layout) or on a 3 x 4 grid (portrait
        // layout), REGARDLESS OF SCREEN RESOLUTION
        _atlasGen.add(s_CARDS_SVG, true, 1);

        // associate each animal type to the respective texture region
        _animalsSprites = new HashMap<>();

        // create the batch (used by 'render' function)
        _batch = new SpriteBatch();

        // create an orthographic camera
        _camera = new OrthographicCamera();

        // let the game intercept input events
        Gdx.input.setInputProcessor(this);

        // display some basic information about installed AmanithSVG library
        amanithsvgInfoDisplay();

        // start a new game (i.e. select random cards and initialize them as 'backside')
        startNewGame();

        // start to display "Powered by AmanithSVG" splash screen (see render loop)
        _clock = new TimerUtil();
        _clock.startTo(s_SHOW_SPLASH_SCREEN, 3000);
    }

    @Override
    public void resize(int width,
                       int height) {
        
        if ((width > 0) && (height > 0)) {

            // get actual backbuffer resolution
            int screenWidth = Gdx.graphics.getBackBufferWidth();
            int screenHeight = Gdx.graphics.getBackBufferHeight();

            // update OpenGL viewport
            HdpiUtils.glViewport(0, 0, width, height);

            // generate "Powered by AmanithSVG" splash screen texture
            generateSplashScreen(screenWidth, screenHeight);

            // generate background texture
            generateBackground(screenWidth, screenHeight);

            // generate congratulation message texture
            generateCongratsMessage(screenWidth, screenHeight);

            // generate animal sprites
            generateAnimalSprites(screenWidth, screenHeight);

            // place cards on the screen
            placeCards(screenWidth, screenHeight);

            // output possible info/warnings/errors from AmanithSVG
            logOutput("AmanithSVG log buffer", true);
        }
    }

    @Override
    public void render() {

        // get actual backbuffer resolution
        int screenWidth = Gdx.graphics.getBackBufferWidth();
        int screenHeight = Gdx.graphics.getBackBufferHeight();

        // setup orthographic camera
        _camera.setToOrtho(false, screenWidth, screenHeight);
        _camera.update();
        _batch.setProjectionMatrix(_camera.combined);

        // need to display "Powered by AmanithSVG" splash screen?
        if (_clock.isTimeTo(s_SHOW_SPLASH_SCREEN)) {
            drawSplashScreen(screenWidth, screenHeight);
        }
        else {
            // is the current game stage completed?
            boolean completed = stageCompleted();

            if (!completed) {
                // game stage not yet completed: draw background and cards
                drawGame(screenWidth, screenHeight);
            }
            else {
                // game stage completed, show the congratulation message if needed
                if (_clock.isTimeTo(s_CONGRATULATE_PLAYER)) {
                    // draw background and congratulation message
                    drawCongratsMessage(screenWidth, screenHeight);
                }
                else {
                    // game stage completed: if we have not congratulated
                    // the player yet, lets start to do it!
                    if (!_clock.lastJobWas(s_CONGRATULATE_PLAYER)) {
                        // start congratulating; the next we enter in this render
                        // function the drawCongratsMessage branch will be chosen
                        _clock.startTo(s_CONGRATULATE_PLAYER, 3000);
                    }
                    else {
                        // game stage completed and we have already congratulated the player
                        // start a new game: shuffle the cards deck and change the background
                        startNewGame();
                        // generate the new background texture
                        generateBackground(screenWidth, screenHeight);
                        // place cards on screen
                        placeCards(screenWidth, screenHeight);
                        // lets start to play again!
                        _clock.startTo(s_PLAY_GAME, 0);
                    }
                }
            }
        }
    }
    
    @Override
    public void dispose() {

        // release AmanithSVG resources
        _splashDoc.dispose();
        for (int i = 0; i < 4; ++i) {
            _backgroundDocs[i].dispose();
        }
        _congratsDoc.dispose();
        _atlas.dispose();
        _atlasGen.dispose();
        // release libGDX resources
        _splashTexture.dispose();
        _backgroundTexture.dispose();
        _congratsTexture.dispose();
        _batch.dispose();
        // release AmanithSVG
        _svg.dispose();
        super.dispose();
    }

    @Override
    public boolean touchDown(int screenX,
                             int screenY,
                             int pointer,
                             int button) {

        boolean processed = false;

        // ignore if it's not left mouse button or first touch pointer
        if ((button == Input.Buttons.LEFT) && (pointer <= 0)) {
            if (!_inputDisabled) {
                // deal with HDPI monitors properly
                int realX = HdpiUtils.toBackBufferX(screenX);
                int realY = HdpiUtils.toBackBufferY(screenY);
                // NB: 2D coordinates (screenX, screenY) relative to the upper left
                // corner of the screen, with the positive x-axis pointing to the
                // right and the y-axis pointing downward. In order to be consistent
                // with the SpriteBatch.draw coordinates system, we flip y coordinate
                selectCard(realX, Gdx.graphics.getBackBufferHeight() - realY);
            }
            processed = true;
        }

        return processed;
    }
    
    @Override
    public boolean touchUp(int screenX,
                           int screenY,
                           int pointer,
                           int button) {
        return ((button == Input.Buttons.LEFT) && (pointer <= 0));
    }

    @Override
    public boolean touchDragged(int screenX,
                                int screenY,
                                int pointer) {
        return false;
    }

    @Override
    public boolean mouseMoved(int screenX,
                              int screenY) {
        return false;
    }

    @Override
    public boolean scrolled(float amountX,
                            float amountY) {
        return false;
    }

    @Override
    public boolean keyDown(int keycode) {
        return false;
    }
    
    @Override
    public boolean keyUp(int keycode) {
        return false;
    }
    
    @Override
    public boolean keyTyped(char character) {
        return false;
    }

    // number of cards
    private static final int s_CARDS_COUNT = 12;

    // cards arrangement when native device orientation is portrait
    private static final int[] s_CARDS_INDEXES_NATIVE_PORTRAIT = {
            0,  1,  2,
            3,  4,  5,
            6,  7,  8,
            9, 10, 11
    };
    // landscape orientation, clockwise from the portrait orientation
    private static final int[] s_CARDS_INDEXES_LANDSCAPE_ROT90 = {
            9, 6, 3, 0,
            10, 7, 4, 1,
            11, 8, 5, 2
    };
    // landscape orientation, counter-clockwise from the portrait orientation
    private static final int[] s_CARDS_INDEXES_LANDSCAPE_ROT270 = {
            2, 5, 8, 11,
            1, 4, 7, 10,
            0, 3, 6,  9
    };

    // cards arrangement when native device orientation is landscape
    private static final int[] s_CARDS_INDEXES_NATIVE_LANDSCAPE = {
            0,  1,  2, 3,
            4,  5,  6, 7,
            8,  9, 10, 11
    };
    private static final int[] s_CARDS_INDEXES_PORTRAIT_ROT90 = {
            8, 4, 0,
            9, 5, 1,
            10, 6, 2,
            11, 7, 3
    };
    private static final int[] s_CARDS_INDEXES_PORTRAIT_ROT270 = {
            3, 7, 11,
            2, 6, 10,
            1, 5, 9,
            0, 4, 8
    };

    private static final String s_SPLASH_SVG = "powBy_AmanithSVG_Dark.svg";
    private static final String s_CONGRATS_SVG = "gameCongrats.svg";
    private static final String s_CARDS_SVG = "gameAnimals.svg";
    private static final String s_LOG_TAG = "Game";
    private static final int s_PLAY_GAME = 1;
    private static final int s_SHOW_SPLASH_SCREEN = 1;
    private static final int s_CONGRATULATE_PLAYER = 2;

    private Input.Orientation _nativeDeviceOrientation;
    // sprite batch
    private SpriteBatch _batch = null;
    // orthographic camera
    private OrthographicCamera _camera = null;
    // instance of AmanithSVG for libGDX
    private SVGAssetsGDX _svg = null;
    // SVG background documents
    private final SVGDocument[] _backgroundDocs = { null, null, null, null };
    // the current background (0..3)
    private int _backgroundIdx = 0;
    // the actual background texture
    private SVGTexture _backgroundTexture = null;
    // SVG document containing a congratulation message
    private SVGDocument _congratsDoc = null;
    // texture containing a congratulation message (created out of the relative SVG document)
    private SVGTexture _congratsTexture = null;
    // splash screen SVG document
    private SVGDocument _splashDoc = null;
    // "Powered by AmanithSVG" texture
    private SVGTexture _splashTexture = null;
    // the utility to handle splash screen and congratulation message timings
    private TimerUtil _clock = null;
    // SVG atlas generator
    private SVGTextureAtlasGenerator _atlasGen = null;
    private SVGTextureAtlas _atlas = null;
    // the scale factor at which animal sprites have been generated
    private float _spritesGenerationScale = -1.0f;
    // associate each animal type the respective texture region
    private HashMap<CardType, SVGTextureAtlasRegion> _animalsSprites = null;
    // the deck of cards
    private final Card[] _cards = new Card[s_CARDS_COUNT];
    // the first selected card (could be null)
    private Card _selectedCard0 = null;
    // the second selected card (could be null)
    private Card _selectedCard1 = null;
    // while waiting (e.g. we are giving time to the player in order to memorize
    // the wrong selected animal couple) we "disable" user touch input
    private boolean _inputDisabled = false;
}
