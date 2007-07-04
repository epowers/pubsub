set PATH=%PATH%;C:\Program Files\Sandcastle\ProductionTools
mrefbuilder /out:WspEventSystem.org PubSubMgr.dll WspEvent.dll WspEventRouter.exe WspSharedQueue.dll
xsltransform /xsl:".\ProductionTransforms\AddOverloads.xsl" WspEventSystem.org /out:out1.xml
xsltransform /xsl:".\ProductionTransforms\AddGuidFilenames.xsl" out1.xml /out:reflection.xml
xsltransform /xsl:".\ProductionTransforms\ReflectionToManifest.xsl" reflection.xml /out:manifest.xml
call copyoutput.bat
call copyhavana.bat
buildassembler /config:sandcastle.config manifest.xml
xsltransform /xsl:".\productiontransforms\ReflectionToChmProject.xsl" reflection.xml /out:output\WspEventSystem.hhp
xsltransform /xsl:".\productiontransforms\ReflectionToChmContents.xsl" reflection.xml /arg:html=output\html /out:output\WspEventSystem.hhc
xsltransform /xsl:".\productiontransforms\ReflectionToChmIndex.xsl" reflection.xml /out:output\WspEventSystem.hhk
cd output
"C:\Program Files\HTML Help Workshop\hhc.exe" WspEventSystem.hhp
