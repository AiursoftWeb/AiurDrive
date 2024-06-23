class Uploader {
    constructor(name, sizeInMb, validExtensions, uploadUrl) {
        this.name = name;
        this.sizeInMb = sizeInMb;
        this.validExtensions = validExtensions;
        this.uploadUrl = uploadUrl;

        this.fileInput = $(`#${name}-file-input`);
        this.progress = $(`#progress-${name}`);
        this.progressbar = $(`#progressbar-${name}`);
        this.addressInput = $(`[name=${name}]`);
    }

    getExtension(filename) {
        var parts = filename.split('.');
        return (parts[parts.length - 1]).toLowerCase();
    }

    reset(that) {
        that.addressInput.val("");
        that.addressInput.attr('data-internet-path', "");
        that.progressbar.css('width', '0%');
        this.progress.addClass('d-none');
        window.onbeforeunload = function () { };
    }

    onFile(that) {
        var file = that.fileInput.prop("files")[0];
        var ext = that.getExtension(file.name);

        if (that.validExtensions.length > 0 && that.validExtensions.indexOf(ext) === -1) {
            return;
        }

        if (file.size / 1024 / 1024 > that.sizeInMb) {
            return;
        }

        window.onbeforeunload = function () {
            return "Your file is still uploading. Are you sure to quit?";
        };

        var formData = new FormData();

        that.progress.removeClass('d-none');
        that.progressbar.css('width', '0%');
        that.progressbar.removeClass('bg-success');
        that.progressbar.addClass('progress-bar-animated');

        formData.append("file", file);
        formData.append("recursiveCreate", true);

        $.ajax({
            url: that.uploadUrl,
            type: 'post',
            enctype: 'multipart/form-data',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            xhr: function () {
                var myXhr = $.ajaxSettings.xhr();
                if (myXhr.upload) {
                    myXhr.upload.addEventListener('progress', function (e) {
                        if (e.lengthComputable) {
                            that.progressbar.css('width', 100 * e.loaded / e.total + '%');
                        }
                    }, false);
                }
                return myXhr;
            },
            success: function (data) {
                window.onbeforeunload = function () { };
                that.addressInput.val(data.internetPath);
                that.addressInput.attr('data-internet-path', data.internetPath);
                that.progressbar.addClass('bg-success');
                that.progressbar.removeClass('progress-bar-animated');
                that.progressbar.css('width', '100%');
            },
            error: that.reset
        });
    }

    init() {
        var that = this;
        this.fileInput.unbind('change');
        this.fileInput.on('change', function() {
            that.onFile(that);
        });
        var dropi = this.fileInput.dropify();
        dropi.on('dropify.afterClear', function() {
            that.reset(that);
        });
    }
}

export default Uploader;