/****************************************************************************
** Copyright (c) 2013-2023 Mazatech S.r.l.
** All rights reserved.
** 
** This file is part of AmanithSVG software, an SVG rendering library.
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

package com.mazatech.svgt;

public enum SVGTError {
    // no error (i.e. the operation was completed successfully)
    None(AmanithSVGJNI.SVGT_NO_ERROR),
    // it indicates that the library has not previously been initialized through the svgtInit() function */
    Uninitialized(AmanithSVGJNI.SVGT_NOT_INITIALIZED_ERROR),
    // returned when one or more invalid document/surface handles are passed to AmanithSVG functions
    BadHandle(AmanithSVGJNI.SVGT_BAD_HANDLE_ERROR),
    // returned when one or more illegal arguments are passed to AmanithSVG functions
    IllegalArgument(AmanithSVGJNI.SVGT_ILLEGAL_ARGUMENT_ERROR),
    // all AmanithSVG functions may signal an "out of memory" error
    OutOfMemory(AmanithSVGJNI.SVGT_OUT_OF_MEMORY_ERROR),
    // returned when an invalid or malformed XML is passed to the svgtDocCreate
    XMLParser(AmanithSVGJNI.SVGT_PARSER_ERROR),
    // returned when a document fragment is technically in error (e.g. if an element has an attribute or property value which is not permissible according to SVG specifications or if the outermost element is not an <svg> element)
    InvalidSVG(AmanithSVGJNI.SVGT_INVALID_SVG_ERROR),
    // returned when a current packing task is still open, and so the operation (e.g. svgtPackingBinInfo) is not allowed
    StillPacking(AmanithSVGJNI.SVGT_STILL_PACKING_ERROR),
    // returned when there isn't a currently open packing task, and so the operation (e.g. svgtPackingAdd) is not allowed
    NotPacking(AmanithSVGJNI.SVGT_NOT_PACKING_ERROR),
    // the specified resource, via svgtResourceSet, is not valid or it does not match the given resource type
    InvalidResource(AmanithSVGJNI.SVGT_INVALID_RESOURCE_ERROR),
    Unknown(AmanithSVGJNI.SVGT_UNKNOWN_ERROR);

    SVGTError(int svgtEnum) {

        _svgtEnum = svgtEnum;
    }

    public int getValue() {

        return _svgtEnum;
    }

    public static SVGTError fromValue(int svgtEnum) {

        return _allValues[svgtEnum];
    }

    private final int _svgtEnum;
    private static final SVGTError[] _allValues = values();
}
