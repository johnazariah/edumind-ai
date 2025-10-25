window.assessmentUi = (function () {
    function enhanceContent(element) {
        // If no element is provided, default to the document body so callers
        // can simply call assessmentUi.enhanceContent() after Blazor renders.
        if (!element) {
            element = document.body;
        }

        if (window.renderMathInElement) {
            try {
                window.renderMathInElement(element, {
                    delimiters: [
                        { left: "$$", right: "$$", display: true },
                        { left: "\\[", right: "\\]", display: true },
                        { left: "\\(", right: "\\)", display: false }
                    ],
                    throwOnError: false
                });
            } catch (err) {
                console.warn("KaTeX rendering failed", err);
            }
        }

        if (window.hljs) {
            element.querySelectorAll("pre code").forEach(function (block) {
                window.hljs.highlightElement(block);
            });
        }
    }

    return {
        enhanceContent: enhanceContent
    };
})();
