<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format" xmlns:fb="http://www.gribuser.ru/xml/fictionbook/2.0" xmlns:xlink="http://www.w3.org/1999/xlink" >
	<xsl:output encoding="UTF-8" method="xml"/>
	<xsl:param name="saveimages" select="2"/>
	<xsl:param name="tocdepth" select="3"/>
	<xsl:param name="NotesTitle" select="'Сноски'"/>
	<xsl:param name="src-name" select="'index.html'"/>
	<xsl:param name="ncx-name" select="'book.ncx'"/>
	<xsl:variable name="CoverID">
		<xsl:choose>
			<xsl:when test="starts-with(//fb:title-info/fb:coverpage/fb:image[1]/@xlink:href,'#')">
				<xsl:value-of select="substring-after(//fb:title-info/fb:coverpage/fb:image[1]/@xlink:href,'#')"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="//fb:title-info/fb:coverpage/fb:image[1]/@xlink:href"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:variable>
	<xsl:template match="/*">
		<package unique-identifier="DOI">
			<metadata>
				<dc-metadata xmlns:dc="http://purl.org/metadata/dublin_core" xmlns:oebpackage="http://openebook.org/namespaces/oeb-package/1.0/">
					<dc:Title>
						<xsl:value-of select="//fb:book-title"/>
					</dc:Title>
					<xsl:for-each select="//fb:description/fb:title-info/fb:author">
						<dc:Creator file-as="{fb:last-name}, {fb:first-name}" role="aut">
							<xsl:value-of select="fb:last-name"/><xsl:text>&#32;</xsl:text>
							<xsl:value-of select="fb:first-name"/><xsl:text>&#32;</xsl:text>
							<xsl:value-of select="fb:middle-name"/>
						</dc:Creator>
					</xsl:for-each>
					<xsl:if test="//fb:description/fb:title-info/fb:date/@value">
						<dc:Date>
							<xsl:value-of select="//fb:description/fb:title-info/fb:date/@value"/>
						</dc:Date>
					</xsl:if>
					<dc:Subject>
						<xsl:value-of select="//fb:description/fb:title-info/fb:genre"/>
					</dc:Subject> 
					<dc:Identifier id="DOI">
						<xsl:choose>
							<xsl:when test="//fb:description/fb:document-info/fb:id and //fb:description/fb:document-info/fb:id != ''"><xsl:value-of select="//fb:description/fb:document-info/fb:id"/></xsl:when>
							<xsl:otherwise>fb2prc_id123456789</xsl:otherwise>
						</xsl:choose>
					</dc:Identifier>
					<dc:Language>
						<xsl:choose>
							<xsl:when test="//fb:description/fb:title-info/fb:lang"><xsl:value-of select="//fb:description/fb:title-info/fb:lang"/></xsl:when>
							<xsl:otherwise>ru</xsl:otherwise>
						</xsl:choose>
					</dc:Language>
					<xsl:if test="//fb:description/fb:title-info/fb:annotation">
						<dc:Description><xsl:value-of select="//fb:description/fb:title-info/fb:annotation"/></dc:Description>
					</xsl:if>
					<xsl:if test="//description/publish-info/publisher">
						<dc:Publisher><xsl:value-of select="//fb:description/fb:publish-info/fb:publisher"/></dc:Publisher>
					</xsl:if>
				</dc-metadata>
				<x-metadata>
					<output encoding="utf-8" />
					<xsl:if test="//fb:title-info/fb:coverpage/fb:image and $saveimages &gt; 0">
						<EmbeddedCover><xsl:value-of select="$CoverID"/></EmbeddedCover>
					</xsl:if>
				</x-metadata>
			</metadata>
			<manifest>
				<item id="content" href="{$src-name}" media-type="text/x-oeb1-document"/>
				<xsl:if test="$saveimages &gt; 0">
					<xsl:apply-templates select="//fb:binary"/>
				</xsl:if>
				<item id="My_Table_of_Contents" media-type="application/x-dtbncx+xml" href="{$ncx-name}"/>
			</manifest>
	      <spine toc="My_Table_of_Contents">
	            <itemref idref="content" />
	      </spine>
	      <xsl:if test="($tocdepth &gt; 0 and count(//fb:body[not(@name) or @name != 'notes']//fb:title) &gt; 1) and (//fb:title-info/fb:coverpage/fb:image and $saveimages &gt; 0)">
				<guide>
					<xsl:if test="//fb:title-info/fb:coverpage/fb:image and $saveimages &gt; 0">
						<reference type="other.ms-coverimage-standard" title="Cover" href="{$CoverID}"/>
					</xsl:if>
					<xsl:if test="$tocdepth &gt; 0 and count(//fb:body[not(@name) or @name != 'notes']//fb:title) &gt; 1">
						<reference type="toc" title="Table of Contents" href="{$src-name}#TOC" />
					</xsl:if>
				</guide>
			</xsl:if>
		</package>
	</xsl:template>
	<xsl:template match="fb:binary">
		<xsl:choose>
			<xsl:when test="@id=$CoverID"><item id="cover" href="{@id}" media-type="{@content-type}"/></xsl:when>
			<xsl:otherwise><item id="{@id}" href="{@id}" media-type="{@content-type}"/></xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>

