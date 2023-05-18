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

#ifndef XML_UTILS_H
#define XML_UTILS_H

/*!
	\file xml_utils.h
	\brief XML serializer, header.
	\author Matteo Muratori
	\author Michele Fabbri
*/

// AmanithSVG
#include <SVGT/svgt.h>

// XML attributes
typedef struct {
	char** names;
	char** values;
	SVGTint count;
} xml_attrs;

// XML node
typedef struct xml_node {
	char* name;
	xml_attrs attrs;
	struct xml_node* childs;
	SVGTint child_count;
	char* content;
} xml_node;

// XML document
typedef struct {
	xml_node* root;
} xml_doc;

// create an XML document for serialization
xml_doc xml_doc_create();

// serialize the given XML document
char* xml_doc_serialize(xml_doc* doc,
						const char* header);

// release the given XML document
void xml_doc_free(xml_doc* doc);

// add a node to the given parent
xml_node* xml_node_add(xml_node* parent,
                       const char* name,
					   const char* content);

// add a string attribute to the given node
void xml_attr_str_add(xml_node* node,
				      const char* name,
				      const char* value);

// add an integer attribute to the given node
void xml_attr_int_add(xml_node* node,
					  const char* name,
					  const SVGTint value);

// add a floating point attribute to the given node
void xml_attr_double_add(xml_node* node,
						 const char* name,
						 const double value);

#endif  /* XML_UTILS_H */
