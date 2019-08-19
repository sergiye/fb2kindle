function InitDropZoneHandlers(maxFileSize) {
    var dropZone = $('#dropZone');
    dropZone.removeClass('error');

    // Check if window.FileReader exists to make
    // sure the browser supports file uploads
    if (typeof (window.FileReader) === 'undefined') {
        dropZone.text('Browser Not Supported!');
        dropZone.addClass('error');
        return;
    }

    // Add a nice drag effect
    dropZone[0].ondragover = function () {
        dropZone.addClass('hover');
        return false;
    };

    // Remove the drag effect when stopping our drag
    dropZone[0].ondragend = function () {
        dropZone.removeClass('hover');
        return false;
    };

    // The drop event handles the file sending
    dropZone[0].ondrop = function (event) {
        // Stop the browser from opening the file in the window
        event.preventDefault();
        dropZone.removeClass('hover');

        // Get the file and the file reader
        var file = event.dataTransfer.files[0];

        // Validate file size
        if (file.size > maxFileSize) {
            dropZone.text('File Too Large!');
            dropZone.addClass('error');
            return false;
        }
        if (!file.name.toLowerCase().endsWith('fb2')) {
            dropZone.text('Try ".fb2" file please!');
            dropZone.addClass('error');
            return false;
        }
        // Send the file
        var xhr = new XMLHttpRequest();
        xhr.upload.addEventListener('progress', function (event) {
            var percent = parseInt(event.loaded / event.total * 100);
            dropZone.text('Uploading: ' + percent + '% wait a bit...');
        }, false);
        xhr.onreadystatechange = function () {
            if (xhr.readyState === 4) {
                if (xhr.status === 200) {
                    var resp = JSON.parse(xhr.response);
                    if (resp.success === true) {
                        dropZone.text('Upload Complete!');
                        var fileLink = '<p><a href="' + resp.link + '" title="Download">' + resp.fileName + '</a></p>';
                        $('#downloadZone')[0].innerHTML += fileLink;
                        //$('#downloadZone').html(fileLink);
                        dropZone.removeClass('error');
                    }
                    else {
                        dropZone.text('Upload error!');
                        dropZone.addClass('error');
                    }
                }
                else {
                    dropZone.text('Upload Failed!');
                    dropZone.addClass('error');
                }
            }
        };
        xhr.open('POST', 'Home/HandleFileUpload', true);
        xhr.setRequestHeader('X-FILE-NAME', encodeURI(file.name));
        xhr.send(file);
        return true;
    };
}

function InitDropZoneHandlers2() {
    var dropZone = document.getElementById('dropZone');
    dropZone.addEventListener('dragover', handleDragOver, false);
    dropZone.addEventListener('drop', handleDnDFileSelect, false);
}

function handleDragOver(event) {
    event.stopPropagation();
    event.preventDefault();
    var dropZone = document.getElementById('dropZone');
    dropZone.innerHTML = "Drop now";
}

function handleDnDFileSelect(event) {
    event.stopPropagation();
    event.preventDefault();

    /* Read the list of all the selected files. */
    var files = event.dataTransfer.files;

    /* Consolidate the output element. */
    var form = document.getElementById('form1');
    var data = new FormData(form);

    for (var i = 0; i < files.length; i++) {
        data.append(files[i].name, files[i]);
    }
    var xhr = new XMLHttpRequest();
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4 && xhr.status === 200 && xhr.responseText) {
            alert("upload done!");
        } else {
            //alert("upload failed!");
        }
    };
    xhr.open('POST', "Upload.aspx");
    // xhr.setRequestHeader("Content-type", "multipart/form-data");
    xhr.send(data);
}