<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1" xmlns:msxsl="urn:schemas-microsoft-com:xslt">

	<xsl:output indent="yes" encoding="UTF-8" />

	<!-- <xsl:key name="typeIndex" match="/reflection/types/type" use="@id" /> -->
	<xsl:key name="memberIndex" match="/*/apis/api" use="@id" />

	<xsl:variable name="attached">
		<xsl:call-template name="getAttached" />
	</xsl:variable>

	<xsl:template match="/">
		<reflection>

			<!-- assemblies and namespaces get copied undisturbed -->
			<xsl:copy-of select="/*/assemblies" />

			<!-- types, with attached members appended to element lists -->
			<apis>
				<!-- copy existing apis, adding attached members to type member lists -->
				<xsl:apply-templates select="/*/apis/api" />

				<!-- add attached members -->
				<xsl:for-each select="msxsl:node-set($attached)/api">
					<xsl:copy-of select="." />
				</xsl:for-each>
			</apis>
		</reflection>
	</xsl:template>

	<xsl:template match="api">
		<xsl:copy-of select="." />
	</xsl:template>

	<xsl:template match="api[apidata/@group='type']">
		<xsl:variable name="typeId" select="@id" />
		<api id="{$typeId}">
			<xsl:copy-of select="*[local-name()!='elements']" />
			<elements>
				<xsl:for-each select="elements/element" >
					<xsl:copy-of select="." />
				</xsl:for-each>
				<xsl:for-each select="msxsl:node-set($attached)/api[containers/type/@api=$typeId]">
					<element api="{@id}" />
				</xsl:for-each>
			</elements>
		</api>
	</xsl:template>

	<xsl:template name="getAttached" >

		<xsl:for-each select="/*/apis/api" >

			<!-- find a static field of type System.Windows.DependencyProperty -->
			<xsl:if test="apidata/@subgroup='field' and memberdata/@static='true' and returns/type/@api='T:System.Windows.DependencyProperty'">
				<xsl:variable name="fieldName" select="apidata/@name" />
				<xsl:variable name="fieldNameLength" select="string-length($fieldName)" />

				<!-- see if the name ends in Property -->
				<xsl:if test="boolean($fieldNameLength &gt; 8) and substring($fieldName,number($fieldNameLength)-7)='Property'">
					<xsl:variable name="propertyName" select="substring($fieldName,1,number($fieldNameLength)-8)" />
					<xsl:variable name="typeName" select="substring(containers/type/@api,3)" />

					<!-- make sure the type doesn't already define this property -->
					<xsl:if test="not(boolean(key('memberIndex',concat('P:',$typeName,'.',$propertyName))))" >	

						<!-- look for getter and setter -->
						<xsl:variable name="getter" select="/*/apis/api[apidata/@name=concat('Get',$propertyName) and apidata/@subgroup='method' and memberdata/@static='true' and count(parameters/parameter)=1 and containers/type/@api=concat('T:',$typeName)][1]" />
						<xsl:variable name="setter" select="/*/apis/api[apidata/@name=concat('Set',$propertyName) and apidata/@subgroup='method' and memberdata/@static='true' and count(parameters/parameter)=2 and containers/type/@api=concat('T:',$typeName)][1]" />

						<xsl:if test="boolean($getter) or boolean($setter)">
							<api id="{concat('P:',$typeName,'.',$propertyName)}">
								<apidata name="{$propertyName}" group="member" subgroup="property" subsubgroup="attachedProperty" />
								<memberdata type="{concat('T:',$typeName)}" visibility="public" static="false" special="false" />
								<proceduredata abstract="false" virtual="false" final="false" />
								<propertydata>
									<xsl:if test="boolean($getter)">
										<xsl:attribute name="get"><xsl:text>true</xsl:text></xsl:attribute>
									</xsl:if>
									<xsl:if test="boolean($setter)">
										<xsl:attribute name="set"><xsl:text>true</xsl:text></xsl:attribute>
									</xsl:if>
								</propertydata>
								<returns>
									<xsl:copy-of select="$getter/returns/*" />
								</returns>
								<containers>
									<xsl:copy-of select="key('memberIndex',concat('T:',$typeName))[1]/containers/namespace" />
									<type api="T:{$typeName}"/>
									<xsl:copy-of select="key('memberIndex',concat('T:',$typeName))[1]/containers/library" />
								</containers>
							</api>
						</xsl:if>

					</xsl:if>

				</xsl:if>
			</xsl:if>

			<xsl:if test="apidata/@subgroup='field' and memberdata/@static='true' and returns/type/@api='T:System.Windows.RoutedEvent'">
				<xsl:variable name="fieldName" select="apidata/@name" />
				<xsl:variable name="fieldNameLength" select="string-length($fieldName)" />

				<!-- see if the name ends in event -->
				<xsl:if test="$fieldNameLength>5 and substring($fieldName,number($fieldNameLength)-4)='Event'">
					<xsl:variable name="eventName" select="substring($fieldName,1,number($fieldNameLength)-5)" />
					<xsl:variable name="typeName" select="substring(containers/type/@api,3)" />

					<!-- make sure the type doesn't already define this event -->
					<xsl:if test="not(boolean(key('memberIndex',concat('E:',$typeName,'.',$eventName))))" >

						<!-- look for the adder and remover -->
						<xsl:variable name="adder" select="/*/apis/api[apidata/@name=concat('Add',$eventName,'Handler') and apidata/@subgroup='method' and containers/type/@api=concat('T:',$typeName) and memberdata/@static='true' and count(parameters/parameter)=2][1]" />
						<xsl:variable name="remover" select="/*/apis/api[apidata/@name=concat('Remove',$eventName,'Handler') and apidata/@subgroup='method' and containers/type/@api=concat('T:',$typeName) and memberdata/@static='true' and count(parameters/parameter)=2][1]" />

						<!-- get event data from the adder and remover -->
						<xsl:variable name="handlerId" select="$adder/parameters/parameter[2]/@type" />
						
						<xsl:if test="boolean($adder) and boolean($remover)">
							<api id="{concat('E:',$typeName,'.',$eventName)}" >
								<apidata name="{$eventName}" group="member" subgroup="event" subsubgroup="attachedEvent" />
								<memberdata visibility="public" static="false" sepcial="false" />
								<proceduredata abstract="false" virtual="false" final="false" />
								<eventdata add="true" remove="true" />
								<eventhandler>
									<xsl:copy-of select="$adder/parameters/parameter[2]/*[1]" />
								</eventhandler>
								<containers>
									<xsl:copy-of select="key('memberIndex',concat('T:',$typeName))[1]/containers/namespace" />
									<type api="T:{$typeName}"/>
									<xsl:copy-of select="key('memberIndex',concat('T:',$typeName))[1]/containers/library" />
								</containers>
							</api>
						</xsl:if>

					</xsl:if>
				</xsl:if>

			</xsl:if>

		</xsl:for-each>

	</xsl:template>

</xsl:stylesheet>
