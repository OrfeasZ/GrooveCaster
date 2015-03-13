var FetchLastBroadcastInfo = function() {
    $.getJSON("/setup/last-broadcast", function (p_Data) {
        var s_Name = p_Data != null ? (p_Data.name || "") : "";
        var s_Description = p_Data != null ? (p_Data.description || "") : "";
        var s_Tag = p_Data != null ? (p_Data.tag) : null;
        var s_TagValue = null;

        if (s_Tag != null)
            s_TagValue = s_Tag.index + ":" + s_Tag.name.toLowerCase();

        var s_SetupContainer = $("#setup-gs-broadcast");

        $.getJSON("/setup/category-tags", function (p_Data) {
            var s_SelectBox = s_SetupContainer.find("#inputBroadcastTag");

            // Sort categories by name.
            p_Data = p_Data.sort(function (a, b) {
                var x = a.name; var y = b.name;
                return ((x < y) ? -1 : ((x > y) ? 1 : 0));
            });

            // Add categories to dropdown.
            for (var i in p_Data) {
                var s_CategoryTag = p_Data[i];
                var s_CategoryTagValue = s_CategoryTag.index + ":" + s_CategoryTag.name;

                var s_Element = $('<option value="' + s_CategoryTagValue.toLowerCase() + '">' + s_CategoryTag.name + '</option>');
                if (s_CategoryTagValue.toLowerCase() === s_TagValue)
                    s_Element.attr("selected", "selected");

                s_SelectBox.append(s_Element);
            }

            // Set last name/description.
            s_SetupContainer.find("#inputBroadcastName").val(s_Name);
            s_SetupContainer.find("#inputBroadcastDesc").val(s_Description);

            $("#gs-last-broadcast-loading").hide();
            s_SetupContainer.show();
        });
    });
};

var PopulateFinishPage = function() {
    var s_Username = $("#inputUsername").val();
    var s_Password = $("#inputPassword").val();
    var s_BroadcastTitle = $("#inputBroadcastName").val();
    var s_BroadcastDesc = $("#inputBroadcastDesc").val();
    var s_BroadcastTagName = $("#inputBroadcastTag option:selected").text();
    var s_BroadcastTag = $("#inputBroadcastTag option:selected").val();

    $("#gs-username-label").text(s_Username);
    $("#bc-name-label").text(s_BroadcastTitle);
    $("#bc-description-label").text(s_BroadcastDesc);
    $("#bc-description-label").text(s_BroadcastDesc);
    $("#bc-category-label").text(s_BroadcastTagName);

    $("#gs-username-input").val(s_Username);
    $("#gs-password-input").val(s_Password);
    $("#bc-title-input").val(s_BroadcastTitle);
    $("#bc-description-input").val(s_BroadcastDesc);
    $("#bc-tag-input").val(s_BroadcastTag);
};

$("#setup-gs-account").ajaxForm({
    dataType: "json",
    success: function (p_Data) {
        $("#gs-account-spinner").hide();
        $("#setup-next-account").removeAttr("disabled");

        switch (p_Data.result) {
            case 0:
                FetchLastBroadcastInfo();
                $('a[href="#broadcast"]').tab("show");
                break;
            case 1:
                $("#gs-account-error").text("Failed to retrieve token data. Please try again.").show();
                break;
            case 3:
                $("#gs-account-error").text("The specified credentials could not be verified.").show();
                break;
            case 4:
                $("#gs-account-error").text("An internal error occurred. Please try again.").show();
                break;
        }
    },
    beforeSubmit: function () {
        $("#setup-next-account").attr("disabled", "disabled");
        $("#gs-account-spinner").show();
        $("#gs-account-error").hide();
    }
});

$("#setup-next-intro").click(function() {
    $('a[href="#account"]').tab("show");
});

$("#setup-next-broadcast").click(function() {
    if ($("#inputBroadcastName").val().length <= 2
        || $("#inputBroadcastDesc").val().length <= 2) {
        $("#gs-broadcast-error").text("Please fill in all the required fields.").show();
        return;
    }

    $("#gs-broadcast-error").hide();

    PopulateFinishPage();
    $('a[href="#finish"]').tab("show");
});