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

/*!
	\file xml_utils.c
	\brief XML serializer, implementation.
	\author Matteo Muratori
	\author Michele Fabbri
*/

#include <stdlib.h>
#include "xml_utils.h"
#include "config.h"

// concatenate a 'src' string to the given 'dst' one, by reallocating memory
static char* xml_strcat(char* dst,
						const char* src) {

	const size_t l = (dst != NULL) ? strlen(dst) : 0U;
	const size_t s = strlen(src);
	char* new_dst = realloc(dst, l + s + 1U);

	if (new_dst != NULL) {
		dst = new_dst;
		(void)memcpy(dst + l, src, s);
		dst[l + s] = '\0';
	}

	return dst;
}

// serialize the given XML node
static void xml_child_serialize(char** data,
								xml_node* n,
								SVGTint nesting_level) {

	SVGTint i;
	// 4 spaces per tab
	const SVGTint spaces = nesting_level * 4;

	// tabulation
	for (i = 0U; i < spaces; ++i) {
		*data = xml_strcat(*data, " ");
	}

	// open node
	*data = xml_strcat(*data, "<");
	*data = xml_strcat(*data, n->name);

	// serialize attributes
	if (n->attrs.count > 0) {
		for (i = 0; i < n->attrs.count; ++i) {
			char* s = malloc(strlen(n->attrs.names[i]) + strlen(n->attrs.values[i]) + 5);
			if (s != NULL) {
				(void)sprintf(s, " %s=\"%s\"", n->attrs.names[i], n->attrs.values[i]);
				*data = xml_strcat(*data, s);
				free(s);
			}
		}
	}

	if ((n->child_count > 0) || (n->content != NULL)) {

		char* s;

		if (n->child_count > 0) {
			// we have children
			*data = xml_strcat(*data, ">\n");
			// serialize children
			for (i = 0; i < n->child_count; ++i) {
				xml_child_serialize(data, n->childs + i, nesting_level + 1);
			}
			// tabulation
			for (i = 0U; i < spaces; ++i) {
				*data = xml_strcat(*data, " ");
			}
		}
		else {
			// no children, but we have content
			*data = xml_strcat(*data, ">");
			*data = xml_strcat(*data, n->content);
		}

		// close the tag
		if ((s = malloc(strlen(n->name) + 6)) != NULL) {
			(void)sprintf(s, "</%s>\n", n->name);
			*data = xml_strcat(*data, s);
			free(s);
		}

	}
	else {
		// no children, no content: close the tag and we have finished with this node
		*data = xml_strcat(*data, " />\n");
	}
}

// create an XML document for serialization
xml_doc xml_doc_create() {
	
	xml_doc doc = { .root = malloc(sizeof(xml_node)) };

	if (doc.root != NULL) {
		doc.root->name = NULL;
		doc.root->attrs = (xml_attrs){ 0 };
		doc.root->childs = NULL;
		doc.root->child_count = 0;
		doc.root->content = NULL;
	}

	return doc;
}

// serialize the given XML document
char* xml_doc_serialize(xml_doc* doc,
						const char* header) {

	xml_node* root = doc->root;
	// start with the given header
	char* data = xml_strcat(NULL, header);
	
	// serialize root's children
	for (SVGTint i = 0; i < root->child_count; ++i) {
		xml_child_serialize(&data, root->childs + i, 0);
	}

	return data;
}

// release allocated memory by the given node
static void xml_node_free(xml_node* node) {

	SVGTint i;

	// recurse on children
	for (i = 0; i < node->child_count; ++i) {
		xml_node_free(node->childs + i);
	}

	if (node->childs != NULL) {
		free(node->childs);
	}

	// free attributes
	if (node->attrs.count > 0) {
		for (i = 0; i < node->attrs.count; ++i) {
			free(node->attrs.names[i]);
			free(node->attrs.values[i]);
		}
		free(node->attrs.names);
		free(node->attrs.values);
	}

	free(node->name);
	if (node->content != NULL) {
		free(node->content);
	}
}

// release the given XML document
void xml_doc_free(xml_doc* doc) {

	// free nodes starting from root
	xml_node_free(doc->root);
	free(doc->root);
}

// add a node to the given parent
xml_node* xml_node_add(xml_node* parent,
                       const char* name,
					   const char* content) {

	xml_node* n = NULL;

	if (parent != NULL) {
		xml_node* new_childs = realloc(parent->childs, (parent->child_count + 1) * sizeof(xml_node));

		char* actualName = calloc(strlen(name) + 1, sizeof(char));
		char* actualContent = (content != NULL) ? calloc(strlen(content) + 1, sizeof(char)) : NULL;

		if ((new_childs != NULL) && (actualName != NULL)) {

			parent->childs = new_childs;
			n = &parent->childs[parent->child_count++];
			//n->name = name;
			(void)strcpy(actualName, name);
			n->name = actualName;

			n->attrs = (xml_attrs){ 0 };
			n->childs = NULL;
			n->child_count = 0;
			//n->content = content;
			if (actualContent != NULL) {
				(void)strcpy(actualContent, content);
			}
			n->content = actualContent;
		}
	}

	return n;
}

// add a string attribute to the given node
void xml_attr_str_add(xml_node* node,
					  const char* name,
				      const char* value) {

	char** new_names = realloc(node->attrs.names, (node->attrs.count + 1) * sizeof(char**));
	char** new_values = realloc(node->attrs.values, (node->attrs.count + 1) * sizeof(char**));

	if ((new_names != NULL) && (new_values != NULL)) {

		char* actualName = calloc(strlen(name) + 1, sizeof(char));
		char* actualValue = calloc(strlen(value) + 1, sizeof(char));

		if ((actualName != NULL) && (actualValue != NULL)) {
			// copy name and value
			(void)strcpy(actualName, name);
			(void)strcpy(actualValue, value);
			// keep track of the new attribute
			node->attrs.names = new_names;
			node->attrs.values = new_values;
			node->attrs.names[node->attrs.count] = actualName;
			node->attrs.values[node->attrs.count] = actualValue;
			node->attrs.count++;
		}
	}
}

// add an integer attribute to the given node
void xml_attr_int_add(xml_node* node,
					  const char* name,
					  const SVGTint value) {

	char v[16] = { 0 };

	// convert integer to string
	(void)snprintf(v, 15, "%d", value);
	// add attribute
	xml_attr_str_add(node, name, v);
}

// add a floating point attribute to the given node
void xml_attr_double_add(xml_node* node,
						 const char* name,
						 const double value) {

	char v[64] = { 0 };

	// convert float number to string
	(void)snprintf(v, 63, "%f", value);
	// add attribute
	xml_attr_str_add(node, name, v);
}