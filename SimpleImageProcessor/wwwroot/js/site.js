var maxWidth, maxHeight;

function resizeImage(img) {
    maxWidth = maxWidth || $('#images').width() - 20;
    maxHeight = maxHeight || $(window).innerHeight() - $('#topBanner').outerHeight() - 40;
    var originalWidth = img.naturalWidth,
        originalHeight = img.naturalHeight,
        ratio = Math.min(maxHeight / originalHeight, maxWidth / originalWidth);
    if (ratio < 1) {
        $(img).css({ 'width': Math.round(originalWidth * ratio) + 'px', 'height': Math.round(originalHeight * ratio) + 'px' });

    }
}
function toggleSizeInput(input, inPixels, inMB) {
    if ($(input).val() === inPixels) {
        $('#sizeInPixels').show();
        $('#sizeInMB').hide();
    } else if ($(input).val() === inMB) {
        $('#sizeInPixels').hide();
        $('#sizeInMB').show();
    } else {
        $('#sizeInPixels').hide();
        $('#sizeInMB').hide();
    }
}