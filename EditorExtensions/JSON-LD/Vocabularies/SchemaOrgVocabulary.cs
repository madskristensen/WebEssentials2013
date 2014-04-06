using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.JSON.Core.Parser;

namespace MadsKristensen.EditorExtensions.JSONLD
{
    [Export(typeof(IVocabulary))]
    class SchemaOrgVocabulary : IVocabulary
    {
        private static Dictionary<string, IEnumerable<Entry>> _cache = BuildCache();
        public Dictionary<string, IEnumerable<Entry>> Cache
        {
            get { return _cache; }
        }

        public string DisplayName
        {
            get { return "http://schema.org"; }
        }

        public bool AppliesToContext(JSONMember contextNode)
        {
            if (contextNode == null || contextNode.Value == null)
                return false;

            return contextNode.Value.Text.Contains("schema.org");
        }

        private static Dictionary<string, IEnumerable<Entry>> BuildCache()
        {
            Dictionary<string, IEnumerable<Entry>> cache = new Dictionary<string, IEnumerable<Entry>>();

            cache["Thing"] = new[] {
                new Entry("additionalType"),
                new Entry("alternateName"),
                new Entry("description"),
                new Entry("image"),
                new Entry("name"),
                new Entry("sameAs"),
                new Entry("url")
            };

            cache["DeliveryMethod"] = cache["Thing"];

            cache["ContactPoint"] = cache["Thing"].Concat(new[] {
                new Entry("areaServed", PropertyType.Object),
                new Entry("availableLanguage", PropertyType.Object),
                new Entry("contactOption", PropertyType.Object),
                new Entry("contactType"),
                new Entry("email"),
                new Entry("faxNumber"),
                new Entry("hoursAvailable", PropertyType.Object),
                new Entry("productSupported", PropertyType.Object),
                new Entry("telephone"),
            });

            cache["PostalAddress"] = cache["ContactPoint"].Concat(new[] {
                new Entry("addressCountry", PropertyType.Object),
                new Entry("addressLocality"),
                new Entry("addressRegion"),
                new Entry("postalCode"),
                new Entry("postOfficeBoxNumber"),
                new Entry("streetAddress"),
            });

            Intangibles(cache);
            CreativeWorks(cache);
            Events(cache);
            Products(cache);


            cache["GeoCoordinates"] = cache["Thing"].Concat(new[] {
                new Entry("elevation"),
                new Entry("latitude"),
                new Entry("longtitude"),
            });

            cache["GeoShape"] = cache["Thing"].Concat(new[] {
                new Entry("box"),
                new Entry("circle"),
                new Entry("elevation"),
                new Entry("line"),
                new Entry("polygon"),
            });

            cache["Organization"] = cache["Thing"].Concat(new[] {
                new Entry("address", PropertyType.Object),
                new Entry("aggregateRating", PropertyType.Object),
                new Entry("brand", PropertyType.Object),
                new Entry("contactPoint", PropertyType.Object),
                new Entry("contactPoints", PropertyType.Object),
                new Entry("department", PropertyType.Object),
                new Entry("duns"),
                new Entry("email"),
                new Entry("employee", PropertyType.Object),
                new Entry("employees", PropertyType.Array),
                new Entry("event", PropertyType.Object),
                new Entry("faxNumber"),
                new Entry("founder", PropertyType.Object),
                new Entry("foundingDate"),
                new Entry("globalLocationNumber"),
                new Entry("hasPOS"),
                new Entry("interactionCount"),
                new Entry("isicV4"),
                new Entry("legalName"),
                new Entry("location", PropertyType.Object),
                new Entry("logo"),
                new Entry("makesOffer", PropertyType.Object),
                new Entry("member", PropertyType.Object),
                new Entry("naics"),
                new Entry("owns", PropertyType.Object),
                new Entry("review", PropertyType.Object),
                new Entry("seeks", PropertyType.Object),
                new Entry("subOrganization", PropertyType.Object),
                new Entry("taxID"),
                new Entry("telephone"),
                new Entry("vatID"),
            });

            cache["Place"] = cache["Thing"].Concat(new[] {
                new Entry("address", PropertyType.Object),
                new Entry("aggregateRating", PropertyType.Object),
                new Entry("containedIn", PropertyType.Object),
                new Entry("event", PropertyType.Object),
                new Entry("faxNumber"),
                new Entry("geo", PropertyType.Object),
                new Entry("globalLocationNumber"),
                new Entry("interactionCount"),
                new Entry("isicV4"),
                new Entry("logo"),
                new Entry("map"),
                new Entry("maps"),
                new Entry("openingHoursSpecification", PropertyType.Object),
                new Entry("photo", PropertyType.Object),
                new Entry("review", PropertyType.Object),
                new Entry("telephone"),
            });

            cache["Person"] = cache["Thing"].Concat(new[] {
                new Entry("additionalName"),
                new Entry("address", PropertyType.Object),
                new Entry("affiliation", PropertyType.Object),
                new Entry("alumniOf"),
                new Entry("award", PropertyType.Object),
                new Entry("birthDate"),
                new Entry("brand", PropertyType.Object),
                new Entry("children", PropertyType.Object),
                new Entry("colleague", PropertyType.Object),
                new Entry("contactPoint", PropertyType.Object),
                new Entry("deatchDate"),
                new Entry("duns"),
                new Entry("email"),
                new Entry("familyName"),
                new Entry("faxNumber"),
                new Entry("follows", PropertyType.Object),
                new Entry("gender"),
                new Entry("givenName"),
                new Entry("globalLocationNumber"),
                new Entry("hasPOS", PropertyType.Object),
                new Entry("homeLocation", PropertyType.Object),
                new Entry("honorificPrefix"),
                new Entry("honorificSuffix"),
                new Entry("interactionCount"),
                new Entry("isicV4"),
                new Entry("jobTitle"),
                new Entry("knows", PropertyType.Object),
                new Entry("makesOffer", PropertyType.Object),
                new Entry("memberOf", PropertyType.Object),
                new Entry("naics", PropertyType.Object),
                new Entry("nationality", PropertyType.Object),
                new Entry("owns", PropertyType.Object),
                new Entry("parent", PropertyType.Object),
                new Entry("performerIn", PropertyType.Object),
                new Entry("relatedTo", PropertyType.Object),
                new Entry("seeks", PropertyType.Object),
                new Entry("sibling", PropertyType.Object),
                new Entry("spouse", PropertyType.Object),
                new Entry("taxID"),
                new Entry("workLocation", PropertyType.Object),
                new Entry("worksFor", PropertyType.Object),
            });

            return cache;
        }

        private static void Intangibles(Dictionary<string, IEnumerable<Entry>> cache)
        {
            cache["Rating"] = cache["Thing"].Concat(new[] {
                new Entry("bestRating"),
                new Entry("ratingValue"),
                new Entry("worstRating"),
            });

            cache["AggregateRating"] = cache["Thing"].Concat(new[] {
                new Entry("itemReviewed", PropertyType.Object),
                new Entry("ratingCount"),
                new Entry("reviewCount"),
            });

            cache["Language"] = cache["Thing"];
        }

        private static void Products(Dictionary<string, IEnumerable<Entry>> cache)
        {
            cache["Product"] = cache["Thing"].Concat(new[] {
                new Entry("agreegateRating", PropertyType.Object),
                new Entry("audience", PropertyType.Object),
                new Entry("brand", PropertyType.Object),
                new Entry("color"),
                new Entry("depth", PropertyType.Object),
                new Entry("gtin13"),
                new Entry("gtin14"),
                new Entry("gtin8"),
                new Entry("height", PropertyType.Object),
                new Entry("isAccesoryOrSeparatePartFor", PropertyType.Object),
                new Entry("isConsumableFor", PropertyType.Object),
                new Entry("isRelatedTo", PropertyType.Object),
                new Entry("isSimilarTo", PropertyType.Object),
                new Entry("itemCondition", PropertyType.Object),
                new Entry("logo", PropertyType.Object),
                new Entry("manufacturer", PropertyType.Object),
                new Entry("model"),
                new Entry("mpn"),
                new Entry("offers", PropertyType.Object),
                new Entry("productID"),
                new Entry("releaseDate"),
                new Entry("review", PropertyType.Object),
                new Entry("sku"),
                new Entry("weight", PropertyType.Object),
                new Entry("width", PropertyType.Object),
            });

            cache["ProductModel"] = cache["Product"].Concat(new[] {
                new Entry("isVariantOf", PropertyType.Object),
                new Entry("predecessorOf", PropertyType.Object),
                new Entry("successorOf", PropertyType.Object),
            });

            cache["IndividualProduct"] = cache["Product"].Concat(new[] {
                new Entry("serialNumber"),
            });

            cache["SomeProducts"] = cache["Product"].Concat(new[] {
                new Entry("inventoryLevel", PropertyType.Object),
            });
        }

        private static void Events(Dictionary<string, IEnumerable<Entry>> cache)
        {
            cache["Event"] = cache["Thing"].Concat(new[] {
                new Entry("attendee", PropertyType.Object),
                new Entry("doorTime"),
                new Entry("duration"),
                new Entry("endDate"),
                new Entry("eventStatus", PropertyType.Object),
                new Entry("location", PropertyType.Object),
                new Entry("offers", PropertyType.Object),
                new Entry("performer", PropertyType.Object),
                new Entry("previousStartDate"),
                new Entry("startDate"),
                new Entry("subEvent", PropertyType.Object),
                new Entry("superEvent", PropertyType.Object),
                new Entry("typicalAgeRange"),
            });

            cache["BusinessEvent"] = cache["Event"];
            cache["ChildrensEvent"] = cache["Event"];
            cache["ComedyEvent"] = cache["Event"];
            cache["DanceEvent"] = cache["Event"];

            cache["DeliveryEvent"] = cache["Event"].Concat(new[] {
                new Entry("accessCode"),
                new Entry("availableFr)om"),
                new Entry("availableThrough"),
                new Entry("hasDeliveryMethod", PropertyType.Object),
            });

            cache["EducationEvent"] = cache["Event"];
            cache["Festival"] = cache["Event"];
            cache["FoodEvent"] = cache["Event"];
            cache["LiteraryEvent"] = cache["Event"];
            cache["MusicEvent"] = cache["Event"];

            cache["PublicationEvent"] = cache["Event"].Concat(new[] {
                new Entry("free"),
                new Entry("publishedOn", PropertyType.Object),
            });

            cache["SalesEvent"] = cache["Event"];
            cache["SocialEvent"] = cache["Event"];
            cache["SportsEvent"] = cache["Event"];
            cache["TheaterEvent"] = cache["Event"];
            cache["UserInteraction"] = cache["Event"];
            cache["VisualArtsEvent"] = cache["Event"];
        }

        private static void CreativeWorks(Dictionary<string, IEnumerable<Entry>> cache)
        {
            cache["CreativeWork"] = cache["Thing"].Concat(new[] {
                new Entry("about", PropertyType.Object),
                new Entry("accessibilityAPI"),
                new Entry("accessibilityControl"),
                new Entry("accessibilityFeature"),
                new Entry("accessibilityHazard"),
                new Entry("accountablePerson", PropertyType.Object),
                new Entry("aggregateRating", PropertyType.Object),
                new Entry("alternativeHeadline"),
                new Entry("associateMedia", PropertyType.Object),
                new Entry("audience", PropertyType.Object),
                new Entry("audio", PropertyType.Object),
                new Entry("author", PropertyType.Object),
                new Entry("award"),
                new Entry("citation"),
                new Entry("comment", PropertyType.Object),
                new Entry("contentLocation", PropertyType.Object),
                new Entry("contentRating"),
                new Entry("contributor", PropertyType.Object),
                new Entry("copyrightHolder", PropertyType.Object),
                new Entry("copyrightYear"),
                new Entry("creator", PropertyType.Object),
                new Entry("dateCreated"),
                new Entry("dateModified"),
                new Entry("datePublished"),
                new Entry("discussionUrl"),
                new Entry("editor"),
                new Entry("educationalAlignment"),
                new Entry("educationalUse"),
                new Entry("encoding", PropertyType.Object),
                new Entry("encodings", PropertyType.Object),
                new Entry("genre"),
                new Entry("headline"),
                new Entry("inLanguage"),
                new Entry("interactionCount"),
                new Entry("interactivityType"),
                new Entry("isBasedOnUrl"),
                new Entry("isFamilyFriendly"),
                new Entry("keywords"),
                new Entry("learningResourceType"),
                new Entry("mentions", PropertyType.Object),
                new Entry("offers", PropertyType.Object),
                new Entry("provider", PropertyType.Object),
                new Entry("publisher", PropertyType.Object),
                new Entry("publishingPrinciples"),
                new Entry("review", PropertyType.Object),
                new Entry("sourceOrganization"),
                new Entry("text"),
                new Entry("thumbnailUrl"),
                new Entry("timeRequired"),
                new Entry("typicalAgeRange"),
                new Entry("version"),
                new Entry("video", PropertyType.Object),
            });

            cache["MediaObject"] = cache["CreativeWork"].Concat(new[] {
                new Entry("associatedArticle", PropertyType.Object),
                new Entry("bitrate"),
                new Entry("contentSize"),
                new Entry("contentUrl"),
                new Entry("duration"),
                new Entry("embedUrl"),
                new Entry("encodesCreativeWork", PropertyType.Object),
                new Entry("encodingFormat"),
                new Entry("expires"),
                new Entry("height", PropertyType.Object),
                new Entry("playerType"),
                new Entry("productionCompany", PropertyType.Object),
                new Entry("publication", PropertyType.Object),
                new Entry("regionsAllowed", PropertyType.Object),
                new Entry("requiresSubscription"),
                new Entry("uploadDate"),
                new Entry("width", PropertyType.Object),
            });

            cache["ImageObject"] = cache["MediaObject"].Concat(new[] {
                new Entry("caption"),
                new Entry("exitData"),
                new Entry("representativeOfPage"),
                new Entry("thumbnail", PropertyType.Object),
            });

            cache["Article"] = cache["CreativeWork"].Concat(new[] {
                new Entry("articleBody"),
                new Entry("articleSection"),
                new Entry("wordCount"),
            });

            cache["AudioObject"] = cache["MediaObject"].Concat(new[] {
                new Entry("transcript"),
            });

            cache["Blog"] = cache["CreativeWork"].Concat(new[] {
                new Entry("blogPost", PropertyType.Object),
            });

            cache["BlogPosting"] = cache["Article"];

            cache["Book"] = cache["CreativeWork"].Concat(new[] {
                new Entry("bookEdition"),
                new Entry("bookFormat", PropertyType.Object),
                new Entry("illustrator", PropertyType.Object),
                new Entry("isbn"),
                new Entry("numberOfPages"),
            });

            cache["Clip"] = cache["CreativeWork"].Concat(new[] {
                new Entry("clipNumber"),
                new Entry("partOfEpisode", PropertyType.Object),
                new Entry("partOfSeason", PropertyType.Object),
                new Entry("partOfSeries", PropertyType.Object),
                new Entry("position"),
                new Entry("publication", PropertyType.Object),
            });

            cache["Code"] = cache["CreativeWork"].Concat(new[] {
                new Entry("codeRepository"),
                new Entry("programmingLanguage"),
                new Entry("runtime"),
                new Entry("sampleType"),
                new Entry("targetProduct", PropertyType.Object),
            });

            cache["Comment"] = cache["CreativeWork"];

            cache["DataCatalog"] = cache["CreativeWork"].Concat(new[] {
                new Entry("dataset", PropertyType.Object),
            });

            cache["Dataset"] = cache["CreativeWork"].Concat(new[] {
                new Entry("catalog", PropertyType.Object),
                new Entry("distribution", PropertyType.Object),
                new Entry("spatial", PropertyType.Object),
                new Entry("temporal"),
            });

            cache["Movie"] = cache["CreativeWork"].Concat(new[] {
                new Entry("actor", PropertyType.Object),
                new Entry("director", PropertyType.Object),
                new Entry("duration"),
                new Entry("musicBy", PropertyType.Object),
                new Entry("producer", PropertyType.Object),
                new Entry("productionCompany", PropertyType.Object),
                new Entry("trailer"),
            });

            cache["Recipe"] = cache["CreativeWork"].Concat(new[] {
                new Entry("cookingMethod"),
                new Entry("cookTime"),
                new Entry("ingredients"),
                new Entry("nutrition", PropertyType.Object),
                new Entry("prepTime"),
                new Entry("recipeCategory"),
                new Entry("recipeCuisine"),
                new Entry("recipeInstructions"),
                new Entry("recipeYield"),
                new Entry("totalTime"),
            });

            cache["Photograph"] = cache["CreativeWork"];

            cache["Review"] = cache["CreativeWork"].Concat(new[] {
                new Entry("itemReviewed", PropertyType.Object),
                new Entry("reviewBody"),
                new Entry("reviewRating", PropertyType.Object),
            });

            cache["SoftwareApplication"] = cache["CreativeWork"].Concat(new[] {
                new Entry("applicationCategory"),
                new Entry("applicationSubCategory"),
                new Entry("applicationSuite"),
                new Entry("countriesNotSupported"),
                new Entry("countriesSupported"),
                new Entry("device"),
                new Entry("downloadUrl"),
                new Entry("featureList"),
                new Entry("fileFormat"),
                new Entry("fileSize"),
                new Entry("installUrl"),
                new Entry("memoryRequirements"),
                new Entry("operatingSystem"),
                new Entry("permissions"),
                new Entry("processorRequirements"),
                new Entry("releaseNotes"),
                new Entry("requirements"),
                new Entry("screenshot", PropertyType.Object),
                new Entry("softwareVersion"),
                new Entry("storageRequirements"),
            });

            cache["MobileApplication"] = cache["SoftwareApplication"].Concat(new[] {
                new Entry("carrierRequirements"),
            });

            cache["WebApplication"] = cache["SoftwareApplication"].Concat(new[] {
                new Entry("browserRequirements"),
            });

            cache["WebPage"] = cache["CreativeWork"].Concat(new[] {
                new Entry("breadcrumb"),
                new Entry("isPartOf"),
                new Entry("lastReviewed"),
                new Entry("mainContentOfPage"),
                new Entry("primaryImageOfPage"),
                new Entry("relatedLink"),
                new Entry("reviewedBy"),
                new Entry("significantLink"),
                new Entry("significantLinks"),
                new Entry("speciality"),
            });

            cache["VideoObject"] = cache["MediaObject"].Concat(new[] {
                new Entry("caption"),
                new Entry("thumbnail", PropertyType.Object),
                new Entry("transcript"),
                new Entry("videoFrameSize"),
                new Entry("videoQuality"),
            });
        }
    }
}