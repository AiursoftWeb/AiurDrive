class Uploader {
    constructor({
                    fileInput,
                    progress,
                    progressbar,
                    addressInput,
                    sizeInMb,
                    validExtensions,
                    uploadUrl,
                    onFile = function () {
                    },
                    onUploaded = function () {
                    },
                    onReset = function () {
                    }
                } = {}) {
        this.fileInput = fileInput;
        this.progress = progress;
        this.progressbar = progressbar;
        this.addressInput = addressInput;
        this.sizeInMb = sizeInMb;
        this.validExtensions = validExtensions;
        this.uploadUrl = uploadUrl;
        this.onFile = onFile;
        this.onUploaded = onUploaded;
        this.onReset = onReset;
        this.onbeforeunloadBackup = window.onbeforeunload;
    }

    getExtension(filename) {
        const parts = filename.split('.');
        return (parts[parts.length - 1]).toLowerCase();
    }

    reset(that) {
        that.addressInput.val("");
        that.progressbar.css('width', '0%');
        that.progress.addClass('d-none');
        window.onbeforeunload = that.onbeforeunloadBackup;
        that.onReset(that);
    }

    tryUpload(that) {
        that.onFile(that);

        const file = that.fileInput.prop("files")[0];
        const ext = that.getExtension(file.name);
        if (that.validExtensions.length > 0 && that.validExtensions.indexOf(ext) === -1) {
            return;
        }

        if (file.size / 1024 / 1024 > that.sizeInMb) {
            return;
        }

        window.onbeforeunload = function () {
            return "Your file is still uploading. Are you sure to quit?";
        };

        that.progress.removeClass('d-none');
        that.progressbar.css('width', '0%');
        that.progressbar.removeClass('bg-success');
        that.progressbar.addClass('progress-bar-animated');

        const formData = new FormData();
        formData.append("file", file);

        $.ajax({
            url: that.uploadUrl,
            type: 'post',
            enctype: 'multipart/form-data',
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            xhr: function () {
                const myXhr = $.ajaxSettings.xhr();
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
                window.onbeforeunload = that.onbeforeunloadBackup;
                that.addressInput.val(data.internetPath);
                that.progressbar.addClass('bg-success');
                that.progressbar.removeClass('progress-bar-animated');
                that.progressbar.css('width', '100%');
                that.onUploaded(data);
            },
            error: that.reset
        });
    }

    init() {
        const that = this;
        that.fileInput.unbind('change');
        that.fileInput.on('change', function () {
            that.tryUpload(that);
        });
        const dropify = that.fileInput.dropify();
        dropify.on('dropify.afterClear', function () {
            that.reset(that);
        });
    }
}

export default Uploader;