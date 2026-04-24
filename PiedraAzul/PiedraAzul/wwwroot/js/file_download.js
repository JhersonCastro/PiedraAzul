window.downloadFile = (filename, contentType, content) => {
    const file = new File([content], filename, { type: contentType });
    const exportUrl = URL.createObjectURL(file);
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = filename;
    a.target = "_self";
    a.click();
    URL.revokeObjectURL(exportUrl);
    a.remove();
}
