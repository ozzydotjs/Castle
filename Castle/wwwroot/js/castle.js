window.castleLyrics = {
    scrollToLine: function (containerElement) {
        if (!containerElement) return;

        const activeElement = containerElement.querySelector('.lyric-line-wrapper.active');
        if (!activeElement) return;

        const containerHeight = containerElement.clientHeight;
        const elementTop = activeElement.offsetTop;
        const scrollTarget = elementTop - (containerHeight / 2) + (activeElement.clientHeight / 2);

        containerElement.scrollTo({
            top: scrollTarget,
            behavior: 'smooth'
        });
    }
};
// Fullscreen management
window.castleFullscreen = {
    dotNetRef: null,

    listenForChanges: function (dotNetRef) {
        this.dotNetRef = dotNetRef;

        const handler = () => {
            const isFullscreen = !!(
                document.fullscreenElement ||
                document.webkitFullscreenElement ||
                document.msFullscreenElement
            );
            dotNetRef.invokeMethodAsync('OnFullscreenChanged', isFullscreen);
        };

        document.addEventListener('fullscreenchange', handler);
        document.addEventListener('webkitfullscreenchange', handler);
        document.addEventListener('msfullscreenchange', handler);
    },

    toggle: function (element) {
        if (!element) return;

        const isFullscreen = !!(
            document.fullscreenElement ||
            document.webkitFullscreenElement ||
            document.msFullscreenElement
        );

        if (isFullscreen) {
            this.exit();
        } else {
            if (element.requestFullscreen) {
                element.requestFullscreen();
            } else if (element.webkitRequestFullscreen) {
                element.webkitRequestFullscreen();
            } else if (element.msRequestFullscreen) {
                element.msRequestFullscreen();
            }
        }
    },

    exit: function () {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    },

    cleanup: function () {
        this.dotNetRef = null;
    }
};