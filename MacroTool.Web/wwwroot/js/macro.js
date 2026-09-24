window.macroTool = {
    scrollToBottom: function (element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },
    closePanel: function () {
        setTimeout(function () {
            window.close();
            setTimeout(function () {
                window.location.replace('about:blank');
            }, 150);
        }, 100);
    }
};
