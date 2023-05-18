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

import com.badlogic.gdx.backends.lwjgl3.Lwjgl3Application;
import com.badlogic.gdx.backends.lwjgl3.Lwjgl3ApplicationConfiguration;

// Please note that on macOS your application needs to be started with the -XstartOnFirstThread JVM argument
public class DesktopLauncher {
    public static void main (String[] arg) {
        Lwjgl3ApplicationConfiguration config = new Lwjgl3ApplicationConfiguration();
        config.setForegroundFPS(60);
        config.setTitle("Game Cards");
        // limit the window size
        config.setWindowSizeLimits(128, 128, 4096, 4096);
        new Lwjgl3Application(new Game(), config);
    }
}