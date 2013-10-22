<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:fb="http://www.gribuser.ru/xml/fictionbook/2.0">
	<xsl:output method="xml" encoding="UTF-8"/>
	<xsl:param name="src-name" select="'index.html'"/>
	<xsl:key name="note-link" match="fb:section" use="@id"/>
	<xsl:template match="/*">
		<!--<!DOCTYPE ncx PUBLIC "-//NISO//DTD ncx 2005-1//EN" "http://www.daisy.org/z3986/2005/ncx-2005-1.dtd">-->
		<ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1" xml:lang="en-US">
		<head>
			<meta name="dtb:uid" content="DOI"/>
			<meta name="dtb:depth" content="2"/>
			<meta name="dtb:totalPageCount" content="0"/>
			<meta name="dtb:maxPageNumber" content="0"/>
		</head>
		<docTitle><text>
			<xsl:value-of select="fb:description/fb:title-info/fb:book-title"/>
		</text></docTitle>
		<docAuthor><text>
			<xsl:value-of select="fb:last-name"/><xsl:text>&#32;</xsl:text>
			<xsl:value-of select="fb:first-name"/><xsl:text>&#32;</xsl:text>
			<xsl:value-of select="fb:middle-name"/>
		</text></docAuthor>
				
		<!-- BUILD navMap -->
		<navMap>
			<navPoint class="toc" id="toc" playOrder="0">
			  <navLabel>
				<text>Table of Contents</text>
			  </navLabel>
			  <content src="{$src-name}#TOC"/>
			</navPoint>
			
			<xsl:apply-templates select="fb:body" mode="toc"/>
		</navMap>
				
	</ncx>			
	</xsl:template>
	<!-- toc template -->
	<xsl:template match="fb:section|fb:body" mode="toc">
		<xsl:choose>
			<xsl:when test="name()='body' and position()=1">
				<xsl:apply-templates select="fb:section" mode="toc"/>
			</xsl:when>
			<xsl:otherwise>
				<navPoint class="section" id="generate-id()" playOrder="">
					<navLabel>
					  <text><xsl:value-of select="normalize-space(fb:title/fb:p[1] | @name)"/></text>
					</navLabel>
					<content src="{$src-name}#TOC_{generate-id()}"/>
				  
					<xsl:if test="fb:section">
						<!--<ul>--><xsl:apply-templates select="fb:section" mode="toc"/><!--</ul>-->
					</xsl:if>
				</navPoint>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	
</xsl:stylesheet>
