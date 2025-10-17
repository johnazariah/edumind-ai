window.assessmentUi = (function () {
    function enhanceContent(element) {
        if (!element) {
            return;
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
